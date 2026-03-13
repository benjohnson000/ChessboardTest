/*
Algorithm:
This reads all 64 board squares and stores the current physical occupied/not-occupied state.
The snapshot is later compared against legal move results to determine what the player actually did.
*/

public sealed class PhysicalBoardSnapshot
{
    private readonly bool[,] occupied = new bool[8, 8];

    public bool this[int rank, int file]
    {
        get => occupied[rank, file];
        set => occupied[rank, file] = value;
    }

    public bool Matches(PhysicalBoardSnapshot other)
    {
        for (int r = 0; r < 8; r++)
        {
            for (int f = 0; f < 8; f++)
            {
                if (occupied[r, f] != other.occupied[r, f])
                    return false;
            }
        }

        return true;
    }

    public int DifferenceCount(PhysicalBoardSnapshot other)
    {
        int diff = 0;

        for (int r = 0; r < 8; r++)
        {
            for (int f = 0; f < 8; f++)
            {
                if (occupied[r, f] != other.occupied[r, f])
                    diff++;
            }
        }

        return diff;
    }
}
