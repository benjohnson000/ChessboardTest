/*
Algorithm:
This adapter sends display output to the Unity simulation button text.
In the Raspberry Pi build, this would be replaced with a real small-screen driver.
*/

public sealed class UnityDisplay : IDisplay
{
    public void ShowMessage(string title, string body)
    {
        SIM_Button.SetText($"{title}: {body}");
    }

    public void ShowStatus(string status)
    {
        SIM_Button.SetText(status);
    }

    public void ShowError(string message)
    {
        SIM_Button.SetText($"ERROR: {message}");
    }

    public void ShowPromotionChoice(PromotionChoice currentChoice)
    {
        SIM_Button.SetText($"Promotion: {currentChoice} (press confirm to cycle / hold to accept later)");
    }

    public void ShowEngineInfo(string info)
    {
        SIM_Button.SetText(info);
    }
}
