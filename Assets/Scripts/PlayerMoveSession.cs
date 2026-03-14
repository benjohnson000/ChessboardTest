/*
Algorithm:
This class stores hints about the move the player is physically making on the board.
It tracks the source square that was picked up and, if applicable, the square of a removed
captured piece. These hints are later used to disambiguate legal moves.
*/

public sealed class PlayerMoveSession
{
    public BoardSquare? SourceSquare { get; private set; }
    public BoardSquare? CaptureSquareHint { get; private set; }

    public void Reset()
    {
        SourceSquare = null;
        CaptureSquareHint = null;
    }

    public void SetSource(BoardSquare square)
    {
        if (!SourceSquare.HasValue)
            SourceSquare = square;
    }

    public void SetCaptureHint(BoardSquare square)
    {
        if (!CaptureSquareHint.HasValue)
            CaptureSquareHint = square;
    }
}
