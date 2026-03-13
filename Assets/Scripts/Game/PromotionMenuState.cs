/*
Algorithm:
This small helper manages cycling through promotion options.
It is useful for a one-button interface where short presses cycle the choice.
*/

public sealed class PromotionMenuState
{
    public PromotionChoice CurrentChoice { get; private set; } = PromotionChoice.Queen;

    public void Reset()
    {
        CurrentChoice = PromotionChoice.Queen;
    }

    public void CycleNext()
    {
        CurrentChoice = CurrentChoice switch
        {
            PromotionChoice.Queen => PromotionChoice.Rook,
            PromotionChoice.Rook => PromotionChoice.Bishop,
            PromotionChoice.Bishop => PromotionChoice.Knight,
            _ => PromotionChoice.Queen
        };
    }
}
