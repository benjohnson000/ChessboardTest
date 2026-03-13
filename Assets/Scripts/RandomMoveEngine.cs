/*
Algorithm:
This engine is a simple fallback for platforms where Stockfish cannot be launched,
such as WebGL. It chooses one legal move at random from the current chess position.
This keeps the rest of the game flow working in browser builds.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RandomMoveEngine : IEngine
{
    public ChessMove GetBestMove(IChessCore chessCore, int thinkTimeMs)
    {
        List<ChessMove> legalMoves = chessCore.GetLegalMoves();

        if (legalMoves == null || legalMoves.Count == 0)
            throw new InvalidOperationException("No legal moves available.");

        int index = UnityEngine.Random.Range(0, legalMoves.Count);
        return legalMoves[index];
    }

    public void Dispose()
    {
        // Nothing to clean up.
    }
}
