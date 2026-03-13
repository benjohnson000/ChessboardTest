using System.Globalization;
using UnityEngine;

/// <summary>
/// Class responsible for interfacing with all leds on chessboard.
/// Internal logic of class will be changed for final project, but method names should remain the same or similar.
/// </summary>
public class LedController
{
    public void SetLedColor(int rankID, int fileID, string color)
    {
        int simRank = 7 - rankID;
        SIM_ChessBoard.instance.GetBoardSpace(simRank, fileID).SetLEDColor(HexToColor(color));
    }

    public void TurnOffLed(int rankID, int fileID)
    {
        int simRank = 7 - rankID;
        SIM_ChessBoard.instance.GetBoardSpace(simRank, fileID).SetLEDColor(Color.clear);
    }

    public void TurnOffAllLeds()
    {
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                TurnOffLed(rank, file);
            }
        }
    }

    private static Color32 HexToColor(string hex)
    {
        hex = hex.Replace("0x", "");
        hex = hex.Replace("#", "");

        byte a = 255;
        byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

        if (hex.Length == 8)
        {
            a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
        }

        return new Color32(r, g, b, a);
    }
}
