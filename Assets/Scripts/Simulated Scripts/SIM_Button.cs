using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity only!!!
/// Should simulate a button that can be pressed to complete an action.
/// </summary>
public class SIM_Button : benjohnson.SIM_Singleton<SIM_Button>
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI text;

    public static void DisableButton()
    {
        instance.button.interactable = false;
    }

    public static void EnableButton()
    {
        instance.button.interactable = true;
    }

    public static void SetText(string text)
    {
        instance.text.SetText(text);
    }
}
