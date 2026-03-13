/// <summary>
/// Class responsible for interfacing with reed switches on the chess board.
/// Logic will need to be changed for final project but method names should remain the same or similar.
/// </summary>
public class ReedSwitchReader
{
    /// <summary>
    /// Returns true if a chess piece is detected at a given rank and file ID.
    /// Chess code uses rank 0 as White's back rank, but the Unity sim stores rank 0 at the top,
    /// so we flip the rank here.
    /// </summary>
    public bool ReadPosition(int rankID, int fileID)
    {
        int simRank = 7 - rankID;
        return SIM_ChessBoard.instance.GetBoardSpace(simRank, fileID).CheckForGamePiece();
    }
}
