/*
Algorithm:
This controller runs the chessboard state machine.
It uses popup buttons for confirm/error/promotion actions, resolves player moves from
physical occupancy, drives engine moves, and waits for board synchronization when needed.
When the board is in an error state, it highlights all mismatched squares in red so the
user can immediately see what needs fixing.

This version includes:
- robust move-session tracking
- castling-safe hinting
- capture-safe fallback resolution for fork situations and varied physical move ordering
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessGameController : MonoBehaviour
{
    private IHardwareBoard hardware;
    private IDisplay display;
    private PhysicalBoardScanner scanner;
    private LedAnimator ledAnimator;
    private BoardSyncService boardSyncService;
    private IChessCore chessCore;
    private PhysicalMoveResolver moveResolver;
    private IEngine engine;
    private ChessSettings settings;

    private GameFlowState state;
    private PhysicalBoardSnapshot latestSnapshot;
    private ChessMove pendingPlayerMove;
    private ChessMove pendingEngineMove;

    private bool pendingGameEndCheckAfterBoardSync;
    private readonly PlayerMoveSession playerMoveSession = new PlayerMoveSession();

    private IEnumerator Start()
    {
        settings = new ChessSettings();

        UnityHardwareBoard unityBoard = new UnityHardwareBoard(new ReedSwitchReader(), new LedController());
        hardware = unityBoard;
        display = new UnityDisplay();

        scanner = new PhysicalBoardScanner(hardware);
        ledAnimator = new LedAnimator(hardware);
        boardSyncService = new BoardSyncService();

        chessCore = ThirdPartyChessFactory.CreateStartingPositionAdapter();
        moveResolver = new PhysicalMoveResolver(ThirdPartyChessFactory.CreateEmptyAdapter);

        engine = CreateEngine();

        state = GameFlowState.Boot;
        display.ShowStatus("Booting...");
        ledAnimator.ShowReadyPattern();

        yield return null;
        yield return new WaitForFixedUpdate();
        yield return null;

        EnterSetupBoardState();
    }

    private void Update()
    {
        latestSnapshot = scanner.Scan();

        switch (state)
        {
            case GameFlowState.SetupBoard:
                TickSetupBoardState();
                break;

            case GameFlowState.WaitingForPlayerBoardChange:
                TickWaitingForPlayerBoardChange();
                break;

            case GameFlowState.WaitingForPlayerConfirm:
                TickWaitingForPlayerConfirm();
                break;

            case GameFlowState.ResolvingPlayerMove:
                ResolvePlayerMove();
                break;

            case GameFlowState.PromotionSelection:
                break;

            case GameFlowState.EngineThinking:
                ResolveEngineMove();
                break;

            case GameFlowState.WaitingForBoardToMatchEngineMove:
                TickWaitingForBoardToMatchEngineMove();
                break;

            case GameFlowState.Error:
                TickErrorState();
                break;
        }
    }

    public void OnConfirmButtonClicked()
    {
        if (hardware is UnityHardwareBoard unityBoard)
        {
            unityBoard.NotifyConfirmPressed();
        }
    }

    private IEngine CreateEngine()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("WebGL build detected. Using RandomMoveEngine.");
        return new RandomMoveEngine();
#else
        try
        {
            string stockfishPath = GetStockfishPath();
            Debug.Log($"Using Stockfish engine at: {stockfishPath}");
            return new StockfishEngine(stockfishPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Stockfish unavailable. Falling back to RandomMoveEngine. Reason: {ex.Message}");
            return new RandomMoveEngine();
        }
#endif
    }

    private void EnterSetupBoardState()
    {
        state = GameFlowState.SetupBoard;
        display.ShowStatus("Setup board in starting position.");
    }

    private void TickSetupBoardState()
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        bool matches = latestSnapshot.Matches(expected);

        ShowSetupBoardFeedback(expected, latestSnapshot, matches);

        if (matches)
        {
            display.ShowStatus("Board ready. Press confirm to start.", OnConfirmButtonClicked);

            if (hardware.ConsumeConfirmPressed())
            {
                display.HidePopup();
                StartPlayerTurn();
            }
        }
        else
        {
            display.ShowStatus("Fix highlighted squares.");
        }
    }

    private void ShowSetupBoardFeedback(PhysicalBoardSnapshot expected, PhysicalBoardSnapshot actual, bool matches)
    {
        hardware.ClearAllLeds();

        if (matches)
        {
            ledAnimator.ShowReadyPattern();
            return;
        }

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                if (expected[rank, file] != actual[rank, file])
                {
                    hardware.SetLed(rank, file, LedTheme.Error);
                }
            }
        }
    }

    private void StartPlayerTurn()
    {
        playerMoveSession.Reset();

        state = GameFlowState.WaitingForPlayerBoardChange;
        display.ShowStatus("Your turn. Move piece, then press confirm.", OnConfirmButtonClicked);
        RefreshPlayerTurnLeds();
    }

    private void TickWaitingForPlayerBoardChange()
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        UpdatePlayerMoveSession(expected, latestSnapshot);

        RefreshPlayerTurnLeds();

        if (hardware.ConsumeConfirmPressed())
        {
            state = GameFlowState.ResolvingPlayerMove;
        }
    }

    private void TickWaitingForPlayerConfirm()
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        UpdatePlayerMoveSession(expected, latestSnapshot);

        RefreshPlayerTurnLeds();

        if (hardware.ConsumeConfirmPressed())
        {
            state = GameFlowState.ResolvingPlayerMove;
        }
    }

    private void RefreshPlayerTurnLeds()
    {
        List<ChessMove> legalMoves = chessCore.GetLegalMoves();

        if (playerMoveSession.SourceSquare.HasValue)
        {
            ledAnimator.ShowLegalDestinations(playerMoveSession.SourceSquare.Value, legalMoves);
        }
        else
        {
            ledAnimator.ShowReadyPattern();
        }

        if (chessCore.IsInCheck(chessCore.SideToMove))
        {
            BoardSquare kingSquare = chessCore.GetKingSquare(chessCore.SideToMove);
            ledAnimator.ShowCheckSquare(kingSquare);
        }
    }

    /*
    Algorithm:
    This tracks live hints about the player's physical move.

    It is intentionally conservative:
    - if exactly one expected square is missing, that is a strong source hint
    - if multiple expected squares are missing, do not guess source too early unless it was already known
    - create a capture hint when the board shape looks like a capture in progress

    Important:
    For a real capture, the key intermediate states may be:
    1) source removed, captured piece still present
    2) source removed, captured piece removed
    3) source placed on target square

    Step 2 produces:
    - 2 missing expected occupied squares
    - 0 newly occupied squares

    So we must treat both newlyOccupied == 0 and newlyOccupied == 1 as valid capture-hint states.
    */
    private void UpdatePlayerMoveSession(PhysicalBoardSnapshot expected, PhysicalBoardSnapshot actual)
    {
        List<BoardSquare> missingExpectedSquares = new List<BoardSquare>();
        List<BoardSquare> newlyOccupiedSquares = new List<BoardSquare>();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                bool shouldBeOccupied = expected[rank, file];
                bool isOccupied = actual[rank, file];

                if (shouldBeOccupied && !isOccupied)
                {
                    missingExpectedSquares.Add(new BoardSquare(rank, file));
                }
                else if (!shouldBeOccupied && isOccupied)
                {
                    newlyOccupiedSquares.Add(new BoardSquare(rank, file));
                }
            }
        }

        // Board matches expected again.
        if (missingExpectedSquares.Count == 0 && newlyOccupiedSquares.Count == 0)
        {
            playerMoveSession.Reset();
            return;
        }

        // Strong source hint: exactly one expected occupied square is now empty.
        if (missingExpectedSquares.Count == 1)
        {
            playerMoveSession.SetSource(missingExpectedSquares[0]);
        }

        // Capture-in-progress shape:
        // - source removed
        // - captured piece removed
        // - optionally destination already re-occupied by moving piece
        //
        // This means:
        // - 2 expected occupied squares are missing
        // - 0 or 1 new square is occupied
        //
        // Castling is not this shape once completed, because it creates 2 newly occupied squares.
        if (missingExpectedSquares.Count == 2 && newlyOccupiedSquares.Count <= 1)
        {
            if (playerMoveSession.SourceSquare.HasValue)
            {
                foreach (BoardSquare square in missingExpectedSquares)
                {
                    if (square != playerMoveSession.SourceSquare.Value)
                    {
                        playerMoveSession.SetCaptureHint(square);
                        break;
                    }
                }
            }
        }
    }

    private void ResolvePlayerMove()
    {
        bool success = moveResolver.TryResolveMove(
            chessCore,
            latestSnapshot,
            out ChessMove resolvedMove,
            out bool requiresPromotionChoice,
            playerMoveSession.SourceSquare,
            playerMoveSession.CaptureSquareHint);

        if (!success)
        {
            state = GameFlowState.Error;
            display.ShowError("Could not resolve move. Fix highlighted squares and press confirm.", OnConfirmButtonClicked);
            ShowBoardMismatchLeds();
            return;
        }

        pendingPlayerMove = resolvedMove;

        if (requiresPromotionChoice)
        {
            if (settings.AutoQueenPromotion)
            {
                pendingPlayerMove = new ChessMove(
                    pendingPlayerMove.From,
                    pendingPlayerMove.To,
                    MoveSpecialType.Promotion,
                    PromotionChoice.Queen);

                CommitPlayerMove();
                return;
            }

            state = GameFlowState.PromotionSelection;
            ledAnimator.ShowPromotionSquare(pendingPlayerMove.To);

            display.ShowPromotionMenu(
                () => OnPromotionSelected(PromotionChoice.Queen),
                () => OnPromotionSelected(PromotionChoice.Rook),
                () => OnPromotionSelected(PromotionChoice.Bishop),
                () => OnPromotionSelected(PromotionChoice.Knight)
            );

            return;
        }

        CommitPlayerMove();
    }

    private void OnPromotionSelected(PromotionChoice choice)
    {
        pendingPlayerMove = new ChessMove(
            pendingPlayerMove.From,
            pendingPlayerMove.To,
            MoveSpecialType.Promotion,
            choice);

        CommitPlayerMove();
    }

    private void CommitPlayerMove()
    {
        display.HidePopup();

        chessCore.ApplyMove(pendingPlayerMove);

        if (CheckForGameEnd())
            return;

        state = GameFlowState.EngineThinking;
        display.ShowEngineInfo("Engine thinking...");
        ledAnimator.Clear();
    }

    private void ResolveEngineMove()
    {
        if (engine == null)
        {
            state = GameFlowState.Error;
            display.ShowError("Engine unavailable.", OnConfirmButtonClicked);
            ShowBoardMismatchLeds();
            return;
        }

        pendingEngineMove = engine.GetBestMove(chessCore, settings.EngineThinkTimeMs);
        chessCore.ApplyMove(pendingEngineMove);

        ledAnimator.ShowMove(pendingEngineMove);
        display.ShowStatus($"Engine move: {pendingEngineMove}. Press confirm after updating the board.", OnConfirmButtonClicked);

        if (settings.RequireBoardSyncAfterEngineMove)
        {
            pendingGameEndCheckAfterBoardSync = true;
            state = GameFlowState.WaitingForBoardToMatchEngineMove;
            return;
        }

        if (CheckForGameEnd())
            return;

        StartPlayerTurn();
    }

    private void TickWaitingForBoardToMatchEngineMove()
    {
        if (!hardware.ConsumeConfirmPressed())
            return;

        if (!boardSyncService.IsBoardSynced(chessCore, latestSnapshot))
        {
            state = GameFlowState.Error;
            display.ShowError("Board does not match expected engine move. Fix highlighted squares and press confirm.", OnConfirmButtonClicked);
            ShowBoardMismatchLeds();
            return;
        }

        if (pendingGameEndCheckAfterBoardSync)
        {
            pendingGameEndCheckAfterBoardSync = false;

            if (CheckForGameEnd())
                return;
        }

        display.HidePopup();
        StartPlayerTurn();
    }

    private void TickErrorState()
    {
        if (!hardware.ConsumeConfirmPressed())
            return;

        if (!boardSyncService.IsBoardSynced(chessCore, latestSnapshot))
        {
            display.ShowError("Still out of sync. Fix highlighted squares and press confirm.", OnConfirmButtonClicked);
            ShowBoardMismatchLeds();
            return;
        }

        display.HidePopup();
        StartPlayerTurn();
    }

    private void ShowBoardMismatchLeds()
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        hardware.ClearAllLeds();

        bool foundMismatch = false;

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                if (expected[rank, file] != latestSnapshot[rank, file])
                {
                    hardware.SetLed(rank, file, LedTheme.Error);
                    foundMismatch = true;
                }
            }
        }

        if (!foundMismatch)
        {
            ledAnimator.ShowErrorCorners();
        }
    }

    private bool CheckForGameEnd()
    {
        if (chessCore.IsCheckmate())
        {
            state = GameFlowState.GameOver;

            PlayerSide losingSide = chessCore.SideToMove;
            PlayerSide winningSide = losingSide == PlayerSide.White ? PlayerSide.Black : PlayerSide.White;

            BoardSquare losingKing = chessCore.GetKingSquare(losingSide);
            BoardSquare winningKing = chessCore.GetKingSquare(winningSide);

            display.ShowMessage("Game Over", "Checkmate", ResetToNewGameSetup);
            ledAnimator.ShowCheckmateKings(winningKing, losingKing);
            return true;
        }

        if (chessCore.IsStalemate())
        {
            state = GameFlowState.GameOver;

            BoardSquare whiteKing = chessCore.GetKingSquare(PlayerSide.White);
            BoardSquare blackKing = chessCore.GetKingSquare(PlayerSide.Black);

            display.ShowMessage("Game Over", "Stalemate", ResetToNewGameSetup);
            ledAnimator.ShowStalemateKings(whiteKing, blackKing);
            return true;
        }

        if (chessCore.IsDraw())
        {
            state = GameFlowState.GameOver;

            BoardSquare whiteKing = chessCore.GetKingSquare(PlayerSide.White);
            BoardSquare blackKing = chessCore.GetKingSquare(PlayerSide.Black);

            display.ShowMessage("Game Over", "Draw", ResetToNewGameSetup);
            ledAnimator.ShowStalemateKings(whiteKing, blackKing);
            return true;
        }

        return false;
    }

    private void ResetToNewGameSetup()
    {
        pendingPlayerMove = null;
        pendingEngineMove = null;
        pendingGameEndCheckAfterBoardSync = false;

        playerMoveSession.Reset();

        chessCore = ThirdPartyChessFactory.CreateStartingPositionAdapter();

        hardware.ClearAllLeds();
        EnterSetupBoardState();
    }


    private string GetStockfishPath()
    {
        return @"C:\Users\user\Documents\GitHub\ChessboardTest\Assets\stockfish\stockfish-windows-x86-64-avx2.exe";
    }

    private void OnDestroy()
    {
        engine?.Dispose();
    }
}
