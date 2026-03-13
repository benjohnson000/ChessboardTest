/*
Algorithm:
This adapter connects the generic hardware interfaces to the Unity simulation objects.
Only this file should need meaningful changes when switching from Unity to Raspberry Pi GPIO.
*/

using UnityEngine;

public sealed class UnityHardwareBoard : IHardwareBoard
{
    private readonly ReedSwitchReader reedReader;
    private readonly LedController ledController;
    private bool confirmPressed;

    public UnityHardwareBoard(ReedSwitchReader reedReader, LedController ledController)
    {
        this.reedReader = reedReader;
        this.ledController = ledController;
    }

    public bool ReadOccupied(int rank, int file) => reedReader.ReadPosition(rank, file);

    public void SetLed(int rank, int file, string hexColor) => ledController.SetLedColor(rank, file, hexColor);

    public void ClearLed(int rank, int file) => ledController.TurnOffLed(rank, file);

    public void ClearAllLeds() => ledController.TurnOffAllLeds();

    public void NotifyConfirmPressed()
    {
        confirmPressed = true;
    }

    public bool ConsumeConfirmPressed()
    {
        bool wasPressed = confirmPressed;
        confirmPressed = false;
        return wasPressed;
    }
}
