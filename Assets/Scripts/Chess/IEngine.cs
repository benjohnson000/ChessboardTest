/*
Algorithm:
This interface represents the chess engine used by the game.
Desktop builds can use Stockfish, while WebGL can use a fallback engine.
The engine picks one move from the current chess position.
*/

using System;

public interface IEngine : IDisposable
{
    ChessMove GetBestMove(IChessCore chessCore, int thinkTimeMs);
}
