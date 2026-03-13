/*
Algorithm:
This file defines the internal chess interfaces and small helper types.
The rest of the project treats these like a stand-in for a third-party library.
Later, you can swap the implementation while keeping the adapter unchanged.
*/

using System.Collections.Generic;

public enum ThirdPartySide
{
    White,
    Black
}

public enum ThirdPartyPromotionPiece
{
    Queen,
    Rook,
    Bishop,
    Knight
}

public sealed class ThirdPartyMove
{
    public int FromRank { get; set; }
    public int FromFile { get; set; }
    public int ToRank { get; set; }
    public int ToFile { get; set; }

    public bool IsCapture { get; set; }
    public bool IsCastleKingSide { get; set; }
    public bool IsCastleQueenSide { get; set; }
    public bool IsEnPassant { get; set; }
    public bool IsPromotion { get; set; }

    public ThirdPartyPromotionPiece PromotionPiece { get; set; }

    public string ToUci()
    {
        string text = $"{ToSquare(FromRank, FromFile)}{ToSquare(ToRank, ToFile)}";
        if (IsPromotion)
        {
            text += PromotionPiece switch
            {
                ThirdPartyPromotionPiece.Queen => "q",
                ThirdPartyPromotionPiece.Rook => "r",
                ThirdPartyPromotionPiece.Bishop => "b",
                ThirdPartyPromotionPiece.Knight => "n",
                _ => "q"
            };
        }
        return text;
    }

    private static string ToSquare(int rank, int file)
    {
        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);
        return $"{fileChar}{rankChar}";
    }
}

public interface IThirdPartyChessPosition
{
    ThirdPartySide SideToMove { get; }
    IEnumerable<ThirdPartyMove> GetLegalMoves();
    void ApplyMove(string uci);
    string GetFen();
    bool IsInCheck(ThirdPartySide side);
    bool IsCheckmate();
    bool IsStalemate();
    bool IsDraw();
    bool IsOccupied(int rank, int file);
}
