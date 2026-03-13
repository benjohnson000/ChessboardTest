/*
Algorithm:
This file centralizes LED colors and highlighting helpers.
It can show ready patterns, selected-piece legal moves, engine moves,
errors, promotions, and check indication.
*/

using System.Collections.Generic;

public sealed class LedAnimator
{
    private readonly IHardwareBoard hardware;

    public LedAnimator(IHardwareBoard hardware)
    {
        this.hardware = hardware;
    }

    public void Clear() => hardware.ClearAllLeds();

    public void ShowReadyPattern()
    {
        hardware.ClearAllLeds();

        for (int f = 0; f < 8; f++)
        {
            hardware.SetLed(0, f, LedTheme.Ready);
            hardware.SetLed(7, f, LedTheme.Ready);
        }

        for (int r = 1; r < 7; r++)
        {
            hardware.SetLed(r, 0, LedTheme.Ready);
            hardware.SetLed(r, 7, LedTheme.Ready);
        }
    }

    public void ShowMove(ChessMove move)
    {
        hardware.ClearAllLeds();
        hardware.SetLed(move.From.Rank, move.From.File, LedTheme.EngineFrom);
        hardware.SetLed(move.To.Rank, move.To.File, LedTheme.EngineTo);

        if (move.SpecialType == MoveSpecialType.Capture || move.SpecialType == MoveSpecialType.EnPassant)
        {
            hardware.SetLed(move.To.Rank, move.To.File, LedTheme.Capture);
        }
    }

    public void ShowLegalDestinations(BoardSquare from, List<ChessMove> legalMoves)
    {
        hardware.ClearAllLeds();
        hardware.SetLed(from.Rank, from.File, LedTheme.SourceSquare);

        foreach (ChessMove move in legalMoves)
        {
            if (move.From == from)
            {
                string color =
                    move.SpecialType == MoveSpecialType.Capture || move.SpecialType == MoveSpecialType.EnPassant
                    ? LedTheme.Capture
                    : move.SpecialType == MoveSpecialType.Promotion
                        ? LedTheme.Promotion
                        : LedTheme.ValidMove;

                hardware.SetLed(move.To.Rank, move.To.File, color);
            }
        }
    }

    public void ShowErrorCorners()
    {
        hardware.ClearAllLeds();
        hardware.SetLed(0, 0, LedTheme.Error);
        hardware.SetLed(0, 7, LedTheme.Error);
        hardware.SetLed(7, 0, LedTheme.Error);
        hardware.SetLed(7, 7, LedTheme.Error);
    }

    public void ShowCheckSquare(BoardSquare kingSquare)
    {
        hardware.SetLed(kingSquare.Rank, kingSquare.File, LedTheme.Check);
    }

    public void ShowPromotionSquare(BoardSquare square)
    {
        hardware.ClearAllLeds();
        hardware.SetLed(square.Rank, square.File, LedTheme.Promotion);
    }

    public void ShowSettingsPattern()
    {
        hardware.ClearAllLeds();

        for (int i = 0; i < 8; i++)
        {
            hardware.SetLed(i, i, LedTheme.Menu);
            hardware.SetLed(i, 7 - i, LedTheme.Menu);
        }
    }
}
