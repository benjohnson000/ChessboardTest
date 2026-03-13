/*
Algorithm:
This defines the hardware contract used by the chess controller.
The main game code only talks to these interfaces.
That makes the same controller work in Unity and on the Raspberry Pi.
*/

public interface IHardwareBoard
{
    bool ReadOccupied(int rank, int file);

    void SetLed(int rank, int file, string hexColor);
    void ClearLed(int rank, int file);
    void ClearAllLeds();

    bool ConsumeConfirmPressed();
}
