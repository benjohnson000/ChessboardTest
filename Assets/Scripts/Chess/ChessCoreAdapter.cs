/*
Algorithm:
This adapter converts the internal chess position format into the project’s public chess API.
It exposes legal moves, board state, occupancy, king location, and game-end checks
while hiding the internal implementation.
*/

using System.Collections.Generic;

public sealed class ChessCoreAdapter : IChessCore, ILoadableFen
{
    private readonly IThirdPartyChessPosition position;

    public ChessCoreAdapter(IThirdPartyChessPosition position)
    {
        this.position = position;
    }

    public PlayerSide SideToMove => position.SideToMove == ThirdPartySide.White ? PlayerSide.White : PlayerSide.Black;

    public string CurrentFen => position.GetFen();

    public List<ChessMove> GetLegalMoves()
    {
        List<ChessMove> result = new List<ChessMove>();

        foreach (ThirdPartyMove move in position.GetLegalMoves())
        {
            MoveSpecialType specialType = MoveSpecialType.Normal;

            if (move.IsCastleKingSide) specialType = MoveSpecialType.CastleKingSide;
            else if (move.IsCastleQueenSide) specialType = MoveSpecialType.CastleQueenSide;
            else if (move.IsEnPassant) specialType = MoveSpecialType.EnPassant;
            else if (move.IsPromotion) specialType = MoveSpecialType.Promotion;
            else if (move.IsCapture) specialType = MoveSpecialType.Capture;

            PromotionChoice? promotion = null;
            if (move.IsPromotion)
            {
                promotion = move.PromotionPiece switch
                {
                    ThirdPartyPromotionPiece.Queen => PromotionChoice.Queen,
                    ThirdPartyPromotionPiece.Rook => PromotionChoice.Rook,
                    ThirdPartyPromotionPiece.Bishop => PromotionChoice.Bishop,
                    ThirdPartyPromotionPiece.Knight => PromotionChoice.Knight,
                    _ => PromotionChoice.Queen
                };
            }

            result.Add(new ChessMove(
                new BoardSquare(move.FromRank, move.FromFile),
                new BoardSquare(move.ToRank, move.ToFile),
                specialType,
                promotion));
        }

        return result;
    }

    public void ApplyMove(ChessMove move)
    {
        position.ApplyMove(move.ToUci());
    }

    public bool IsInCheck(PlayerSide side)
    {
        ThirdPartySide internalSide = side == PlayerSide.White ? ThirdPartySide.White : ThirdPartySide.Black;
        return position.IsInCheck(internalSide);
    }

    public bool IsCheckmate() => position.IsCheckmate();
    public bool IsStalemate() => position.IsStalemate();
    public bool IsDraw() => position.IsDraw();

    public PhysicalBoardSnapshot GetExpectedOccupancySnapshot()
    {
        PhysicalBoardSnapshot snapshot = new PhysicalBoardSnapshot();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                snapshot[rank, file] = position.IsOccupied(rank, file);
            }
        }

        return snapshot;
    }

    public BoardSquare GetKingSquare(PlayerSide side)
    {
        ThirdPartySide internalSide = side == PlayerSide.White ? ThirdPartySide.White : ThirdPartySide.Black;

        if (position is InternalChessPosition internalPosition)
        {
            internalPosition.FindKingSquare(internalSide, out int rank, out int file);
            return new BoardSquare(rank, file);
        }

        throw new System.NotSupportedException("Underlying position does not support king square lookup.");
    }

    public void LoadFen(string fen)
    {
        if (position is ILoadableFen loadable)
        {
            loadable.LoadFen(fen);
            return;
        }

        throw new System.NotSupportedException("Underlying position does not support FEN loading.");
    }
}
