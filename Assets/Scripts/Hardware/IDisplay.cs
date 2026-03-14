using System;

public interface IDisplay
{
    void ShowMessage(string title, string body, Action onOk = null);

    void ShowStatus(string status, Action onConfirm = null);

    void ShowError(string message, Action onOk = null);

    void ShowPromotionMenu(
        Action onQueen,
        Action onRook,
        Action onBishop,
        Action onKnight);

    void HidePopup();
    void ShowEngineInfo(string info);
}
