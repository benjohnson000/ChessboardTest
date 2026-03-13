/*
Algorithm:
This compares the expected software position to the real board occupancy.
It is used at startup, after engine moves, and when recovering from player mistakes.
*/

public sealed class BoardSyncService
{
    public bool IsBoardSynced(IChessCore chessCore, PhysicalBoardSnapshot currentSnapshot)
    {
        PhysicalBoardSnapshot expected = chessCore.GetExpectedOccupancySnapshot();
        return expected.Matches(currentSnapshot);
    }
}
