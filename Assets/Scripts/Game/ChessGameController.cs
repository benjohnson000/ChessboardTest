/*
Algorithm:
This controller runs the chessboard state machine.
It creates a platform-appropriate engine: Stockfish on desktop/editor,
and a random legal-move fallback on WebGL. This allows browser builds
to work without requiring the user to install Stockfish.
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
    private PromotionChoice currentPromotionChoice = PromotionChoice.Queen;

    private bool pendingGameEndCheckAfterBoardSync;

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
                TickPromotionSelection();
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
        display.ShowMessage("Setup Board", "Place pieces in starting position.");
    }

    private void TickSetupBoardState()
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        bool matches = latestSnapshot.Matches(expected);

        ShowSetupBoardFeedback(expected, latestSnapshot, matches);

        if (matches)
        {
            display.ShowStatus("Board ready. Press confirm to start.");

            if (hardware.ConsumeConfirmPressed())
            {
                StartPlayerTurn();
            }
        }
        else
        {
            display.ShowMessage("Setup Board", "Fix highlighted squares.");
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
        state = GameFlowState.WaitingForPlayerBoardChange;
        display.ShowStatus("Your turn. Move piece, then press confirm.");
        RefreshPlayerTurnLeds();
    }

    private void TickWaitingForPlayerBoardChange()
    {
        RefreshPlayerTurnLeds();

        if (hardware.ConsumeConfirmPressed())
        {
            state = GameFlowState.ResolvingPlayerMove;
        }
    }

    private void TickWaitingForPlayerConfirm()
    {
        RefreshPlayerTurnLeds();

        if (hardware.ConsumeConfirmPressed())
        {
            state = GameFlowState.ResolvingPlayerMove;
        }
    }

    private void RefreshPlayerTurnLeds()
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        List<ChessMove> legalMoves = chessCore.GetLegalMoves();

        BoardSquare? pickedUpSquare = TryGetPickedUpSquare(expected, latestSnapshot);

        if (pickedUpSquare.HasValue)
        {
            ledAnimator.ShowLegalDestinations(pickedUpSquare.Value, legalMoves);
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

    private BoardSquare? TryGetPickedUpSquare(PhysicalBoardSnapshot expected, PhysicalBoardSnapshot actual)
    {
        List<BoardSquare> missingExpectedPieces = new List<BoardSquare>();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                if (expected[rank, file] && !actual[rank, file])
                {
                    missingExpectedPieces.Add(new BoardSquare(rank, file));
                }
            }
        }

        if (missingExpectedPieces.Count == 1)
            return missingExpectedPieces[0];

        return null;
    }

    private void ResolvePlayerMove()
    {
        bool success = moveResolver.TryResolveMove(chessCore, latestSnapshot, out ChessMove resolvedMove, out bool requiresPromotionChoice);

        if (!success)
        {
            state = GameFlowState.Error;
            display.ShowError("Could not resolve move. Check board and press confirm.");
            ledAnimator.ShowErrorCorners();
            return;
        }

        pendingPlayerMove = resolvedMove;

        if (requiresPromotionChoice)
        {
            if (settings.AutoQueenPromotion)
            {
                pendingPlayerMove = new ChessMove(pendingPlayerMove.From, pendingPlayerMove.To, MoveSpecialType.Promotion, PromotionChoice.Queen);
                CommitPlayerMove();
                return;
            }

            currentPromotionChoice = PromotionChoice.Queen;
            state = GameFlowState.PromotionSelection;
            display.ShowPromotionChoice(currentPromotionChoice);
            ledAnimator.ShowPromotionSquare(pendingPlayerMove.To);
            return;
        }

        CommitPlayerMove();
    }

    private void TickPromotionSelection()
    {
        if (hardware.ConsumeConfirmPressed())
        {
            currentPromotionChoice = NextPromotion(currentPromotionChoice);
            display.ShowPromotionChoice(currentPromotionChoice);

            pendingPlayerMove = new ChessMove(
                pendingPlayerMove.From,
                pendingPlayerMove.To,
                MoveSpecialType.Promotion,
                currentPromotionChoice);
        }
    }

    public void AcceptPromotionChoice()
    {
        CommitPlayerMove();
    }

    private void CommitPlayerMove()
    {
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
            display.ShowError("Engine unavailable.");
            ledAnimator.ShowErrorCorners();
            return;
        }

        pendingEngineMove = engine.GetBestMove(chessCore, settings.EngineThinkTimeMs);
        chessCore.ApplyMove(pendingEngineMove);

        ledAnimator.ShowMove(pendingEngineMove);
        display.ShowEngineInfo($"Engine move: {pendingEngineMove}");

        if (settings.RequireBoardSyncAfterEngineMove)
        {
            pendingGameEndCheckAfterBoardSync = true;
            state = GameFlowState.WaitingForBoardToMatchEngineMove;
            display.ShowStatus("Make the engine move on the board, then press confirm.");
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
            display.ShowError("Board does not match expected engine move.");
            ledAnimator.ShowErrorCorners();
            return;
        }

        if (pendingGameEndCheckAfterBoardSync)
        {
            pendingGameEndCheckAfterBoardSync = false;

            if (CheckForGameEnd())
                return;
        }

        StartPlayerTurn();
    }

    private void TickErrorState()
    {
        if (!hardware.ConsumeConfirmPressed())
            return;

        if (!boardSyncService.IsBoardSynced(chessCore, latestSnapshot))
        {
            display.ShowError("Still out of sync.");
            ledAnimator.ShowErrorCorners();
            return;
        }

        StartPlayerTurn();
    }

    private bool CheckForGameEnd()
    {
        if (chessCore.IsCheckmate())
        {
            state = GameFlowState.GameOver;
            display.ShowMessage("Game Over", "Checkmate");
            ledAnimator.Clear();
            return true;
        }

        if (chessCore.IsStalemate())
        {
            state = GameFlowState.GameOver;
            display.ShowMessage("Game Over", "Stalemate");
            ledAnimator.Clear();
            return true;
        }

        if (chessCore.IsDraw())
        {
            state = GameFlowState.GameOver;
            display.ShowMessage("Game Over", "Draw");
            ledAnimator.Clear();
            return true;
        }

        return false;
    }

    private PromotionChoice NextPromotion(PromotionChoice current)
    {
        return current switch
        {
            PromotionChoice.Queen => PromotionChoice.Rook,
            PromotionChoice.Rook => PromotionChoice.Bishop,
            PromotionChoice.Bishop => PromotionChoice.Knight,
            _ => PromotionChoice.Queen
        };
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
