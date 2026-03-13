/*
Algorithm:
This interface wraps a real chess rules engine.
It exposes just what the physical board project needs:
legal moves, position updates, FEN, and game-over checks.
*/

using System.Collections.Generic;

public interface IChessCore
{
    PlayerSide SideToMove { get; }
    string CurrentFen { get; }

    List<ChessMove> GetLegalMoves();
    void ApplyMove(ChessMove move);

    bool IsInCheck(PlayerSide side);
    bool IsCheckmate();
    bool IsStalemate();
    bool IsDraw();

    PhysicalBoardSnapshot GetExpectedOccupancySnapshot();
    BoardSquare GetKingSquare(PlayerSide side);
}

public interface ILoadableFen
{
    void LoadFen(string fen);
}
