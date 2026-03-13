/*
Algorithm:
This file centralizes common display strings used by the board.
That keeps the controller cleaner and makes it easier to update wording later.
*/

public static class DisplayMessages
{
    public const string Booting = "Booting...";
    public const string PlayerTurn = "Your turn. Move piece, then press confirm.";
    public const string EngineThinking = "Engine thinking...";
    public const string ApplyEngineMove = "Make the engine move on the board, then press confirm.";
    public const string StartSyncError = "Board does not match starting position.";
    public const string ResolveMoveError = "Could not resolve move. Check board and press confirm.";
    public const string EngineSyncError = "Board does not match expected engine move.";
    public const string StillOutOfSync = "Still out of sync.";
    public const string Checkmate = "Checkmate";
    public const string Stalemate = "Stalemate";
    public const string Draw = "Draw";
}
