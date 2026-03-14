using System;

/// <summary>
/// UNITY ONLY!!!
/// Adapts the generic display interface to the simulated popup UI.
/// </summary>
public sealed class UnityDisplay : IDisplay
{
    public void ShowMessage(string title, string body, Action onOk = null)
    {
        SIM_ScreenController.instance.ShowMenuForced(
            $"{title}\n{body}",
            new SIM_ScreenController.ButtonDefinition("OK", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onOk?.Invoke();
            }));
    }

    public void ShowStatus(string status, Action onConfirm = null)
    {
        if (onConfirm == null)
        {
            SIM_ScreenController.instance.ShowMessage(status);
            SIM_ScreenController.instance.DestroyAllButtons();
            return;
        }

        SIM_ScreenController.instance.ShowMenu(
            status,
            new SIM_ScreenController.ButtonDefinition("Confirm", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onConfirm?.Invoke();
            }));
    }

    public void ShowError(string message, Action onOk = null)
    {
        SIM_ScreenController.instance.ShowMenuForced(
            $"ERROR\n{message}",
            new SIM_ScreenController.ButtonDefinition("OK", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onOk?.Invoke();
            }));
    }

    public void ShowPromotionMenu(
        Action onQueen,
        Action onRook,
        Action onBishop,
        Action onKnight)
    {
        SIM_ScreenController.instance.ShowMenuForced(
            "Choose promotion:",
            new SIM_ScreenController.ButtonDefinition("Queen", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onQueen?.Invoke();
            }),
            new SIM_ScreenController.ButtonDefinition("Rook", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onRook?.Invoke();
            }),
            new SIM_ScreenController.ButtonDefinition("Bishop", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onBishop?.Invoke();
            }),
            new SIM_ScreenController.ButtonDefinition("Knight", () =>
            {
                SIM_ScreenController.instance.ClearAndHide();
                onKnight?.Invoke();
            })
        );
    }

    public void HidePopup()
    {
        SIM_ScreenController.instance.ClearAndHide();
    }

    public void ShowEngineInfo(string info)
    {
        SIM_ScreenController.instance.ShowMessage(info);
        SIM_ScreenController.instance.DestroyAllButtons();
    }
}
