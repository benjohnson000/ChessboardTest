using System;

public enum PlayerSide
{
    White,
    Black
}

public enum MoveSpecialType
{
    Normal,
    Capture,
    CastleKingSide,
    CastleQueenSide,
    EnPassant,
    Promotion
}

public enum PromotionChoice
{
    Queen,
    Rook,
    Bishop,
    Knight
}

public enum GameFlowState
{
    Boot,
    SetupBoard,
    WaitingForPlayerBoardChange,
    WaitingForPlayerConfirm,
    ResolvingPlayerMove,
    PromotionSelection,
    EngineThinking,
    ShowingEngineMove,
    WaitingForBoardToMatchEngineMove,
    Error,
    GameOver,
    SettingsMenu
}

public readonly struct BoardSquare : IEquatable<BoardSquare>
{
    public int Rank { get; }
    public int File { get; }

    public BoardSquare(int rank, int file)
    {
        Rank = rank;
        File = file;
    }

    public bool IsValid => Rank >= 0 && Rank < 8 && File >= 0 && File < 8;

    public override string ToString()
    {
        char fileChar = (char)('a' + File);
        int rankNum = Rank + 1;
        return $"{fileChar}{rankNum}";
    }

    public bool Equals(BoardSquare other) => Rank == other.Rank && File == other.File;
    public override bool Equals(object obj) => obj is BoardSquare other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Rank, File);

    public static bool operator ==(BoardSquare a, BoardSquare b) => a.Equals(b);
    public static bool operator !=(BoardSquare a, BoardSquare b) => !a.Equals(b);
}

public sealed class ChessMove
{
    public BoardSquare From { get; }
    public BoardSquare To { get; }
    public MoveSpecialType SpecialType { get; }
    public PromotionChoice? Promotion { get; }

    public ChessMove(BoardSquare from, BoardSquare to, MoveSpecialType specialType = MoveSpecialType.Normal, PromotionChoice? promotion = null)
    {
        From = from;
        To = to;
        SpecialType = specialType;
        Promotion = promotion;
    }

    public string ToUci()
    {
        string text = $"{From}{To}";
        if (Promotion.HasValue)
        {
            text += Promotion.Value switch
            {
                PromotionChoice.Queen => "q",
                PromotionChoice.Rook => "r",
                PromotionChoice.Bishop => "b",
                PromotionChoice.Knight => "n",
                _ => ""
            };
        }
        return text;
    }

    public override string ToString() => ToUci();
}
