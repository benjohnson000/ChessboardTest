/*
Algorithm:
This class resolves the player's move by comparing the physical board snapshot
to the result of every legal move from the current software position.

It tries multiple passes:
1) use both source and capture hints
2) use only source hint
3) use no hints at all

This makes the system much more robust for real physical move ordering, especially
for captures where the player may remove the captured piece before or after moving
their own piece. Castling still works because it can benefit from hints when valid,
but the resolver no longer gets stuck if a hint was recorded incorrectly.
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

    public bool TryResolveMove(
        IChessCore currentPosition,
        PhysicalBoardSnapshot realSnapshot,
        out ChessMove resolvedMove,
        out bool requiresPromotionChoice,
        BoardSquare? sourceHint = null,
        BoardSquare? captureHint = null)
    {
        // Pass 1: use full hints
        if (TryResolveMoveInternal(currentPosition, realSnapshot, out resolvedMove, out requiresPromotionChoice, sourceHint, captureHint))
            return true;

        // Pass 2: use only source hint
        if (sourceHint.HasValue && TryResolveMoveInternal(currentPosition, realSnapshot, out resolvedMove, out requiresPromotionChoice, sourceHint, null))
            return true;

        // Pass 3: no hints at all
        if (TryResolveMoveInternal(currentPosition, realSnapshot, out resolvedMove, out requiresPromotionChoice, null, null))
            return true;

        resolvedMove = null;
        requiresPromotionChoice = false;
        return false;
    }

    private bool TryResolveMoveInternal(
        IChessCore currentPosition,
        PhysicalBoardSnapshot realSnapshot,
        out ChessMove resolvedMove,
        out bool requiresPromotionChoice,
        BoardSquare? sourceHint,
        BoardSquare? captureHint)
    {
        List<ChessMove> legalMoves = currentPosition.GetLegalMoves();
        List<ChessMove> matches = new List<ChessMove>();

        foreach (ChessMove move in legalMoves)
        {
            if (sourceHint.HasValue && move.From != sourceHint.Value)
                continue;

            if (captureHint.HasValue)
            {
                bool isCastle =
                    move.SpecialType == MoveSpecialType.CastleKingSide ||
                    move.SpecialType == MoveSpecialType.CastleQueenSide;

                bool usesCaptureHint =
                    (move.SpecialType == MoveSpecialType.Capture && move.To == captureHint.Value) ||
                    (move.SpecialType == MoveSpecialType.Promotion && move.To == captureHint.Value) ||
                    (move.SpecialType == MoveSpecialType.EnPassant);

                // Only filter by capture hint for actual capture-like moves.
                if (!isCastle && !usesCaptureHint)
                    continue;
            }

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
