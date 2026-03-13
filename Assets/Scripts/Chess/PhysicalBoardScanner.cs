/*
Algorithm:
This scans the hardware board and produces a snapshot of occupied squares.
The rest of the system can compare these snapshots against expected legal positions.
*/

public sealed class PhysicalBoardScanner
{
    private readonly IHardwareBoard hardware;

    public PhysicalBoardScanner(IHardwareBoard hardware)
    {
        this.hardware = hardware;
    }

    public PhysicalBoardSnapshot Scan()
    {
        PhysicalBoardSnapshot snapshot = new PhysicalBoardSnapshot();

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                snapshot[rank, file] = hardware.ReadOccupied(rank, file);
            }
        }

        return snapshot;
    }
}
