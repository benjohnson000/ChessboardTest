/*
Algorithm:
This is the shape of the Raspberry Pi adapter.
It reads actual GPIO inputs for reed switches and writes LED data to the LED chain.
The chess controller above should not need meaningful changes.
*/

public sealed class PiHardwareBoard : IHardwareBoard
{
    public bool ReadOccupied(int rank, int file)
    {
        // Read GPIO / mux / matrix input here
        throw new System.NotImplementedException();
    }

    public void SetLed(int rank, int file, string hexColor)
    {
        // Write to LED strip buffer here
        throw new System.NotImplementedException();
    }

    public void ClearLed(int rank, int file)
    {
        // Clear one LED in the buffer here
        throw new System.NotImplementedException();
    }

    public void ClearAllLeds()
    {
        // Clear the whole LED chain here
        throw new System.NotImplementedException();
    }

    public bool ConsumeConfirmPressed()
    {
        // Return true once when the physical confirm button is pressed
        throw new System.NotImplementedException();
    }
}
