/*
Algorithm:
This class resolves the player’s move by comparing the physical board snapshot
to the result of every legal move from the current software position.
If exactly one legal move matches the occupancy, that must be the move played.
*/

using System;
using System.Collections.Generic;

public sealed class PhysicalMoveResolver
{
    private readonly Func<IChessCore> chessCoreFactory;

    public PhysicalMoveResolver(Func<IChessCore> chessCoreFactory)
    {
        this.chessCoreFactory = chessCoreFactory;
    }

    public bool TryResolveMove(IChessCore currentPosition, PhysicalBoardSnapshot realSnapshot, out ChessMove resolvedMove, out bool requiresPromotionChoice)
    {
        List<ChessMove> legalMoves = currentPosition.GetLegalMoves();
        List<ChessMove> matches = new List<ChessMove>();

        foreach (ChessMove move in legalMoves)
        {
            IChessCore copy = chessCoreFactory();

            if (copy is not ILoadableFen loadable)
                throw new InvalidOperationException("Chess core copy must support FEN loading.");

            loadable.LoadFen(currentPosition.CurrentFen);
            copy.ApplyMove(move);

            PhysicalBoardSnapshot expected = copy.GetExpectedOccupancySnapshot();
            if (expected.Matches(realSnapshot))
            {
                matches.Add(move);
            }
        }

        if (matches.Count == 1)
        {
            resolvedMove = matches[0];
            requiresPromotionChoice = false;
            return true;
        }

        if (matches.Count > 1 && AreOnlyDifferentByPromotion(matches))
        {
            resolvedMove = new ChessMove(matches[0].From, matches[0].To, MoveSpecialType.Promotion, null);
            requiresPromotionChoice = true;
            return true;
        }

        resolvedMove = null;
        requiresPromotionChoice = false;
        return false;
    }

    private bool AreOnlyDifferentByPromotion(List<ChessMove> moves)
    {
        if (moves.Count == 0)
            return false;

        BoardSquare from = moves[0].From;
        BoardSquare to = moves[0].To;

        foreach (ChessMove move in moves)
        {
            if (move.SpecialType != MoveSpecialType.Promotion)
                return false;

            if (move.From != from || move.To != to)
                return false;
        }

        return true;
    }
}
