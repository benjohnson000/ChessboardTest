/*
Algorithm:
This defines the small screen interface used by the chess controller.
The Unity sim and Raspberry Pi display can both implement this contract.
*/

public interface IDisplay
{
    void ShowMessage(string title, string body);
    void ShowStatus(string status);
    void ShowError(string message);
    void ShowPromotionChoice(PromotionChoice currentChoice);
    void ShowEngineInfo(string info);
}
