/*
Algorithm:
This holds the configurable options for the chessboard.
The small screen can edit these later through a menu system.
*/

public sealed class ChessSettings
{
    public PlayerSide HumanSide { get; set; } = PlayerSide.White;
    public int EngineThinkTimeMs { get; set; } = 300;
    public bool ShowLegalMoves { get; set; } = true;
    public bool ShowHints { get; set; } = false;
    public bool RequireBoardSyncAfterEngineMove { get; set; } = true;
    public bool AutoQueenPromotion { get; set; } = false;
}
