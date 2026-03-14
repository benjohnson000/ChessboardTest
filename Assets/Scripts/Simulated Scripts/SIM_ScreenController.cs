using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// UNITY ONLY!!!
/// Simulates a screen popup with message text and a variable number of buttons.
/// Access singleton from SIM_ScreenController.instance
/// </summary>
public class SIM_ScreenController : benjohnson.SIM_Singleton<SIM_ScreenController>
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Transform buttonsParent;

    private readonly List<SIM_Button> activeButtons = new List<SIM_Button>();

    private string lastMenuSignature = null;
    private string lastMessageOnlyText = null;

    protected override void Awake()
    {
        base.Awake();

        if (root == null)
            root = gameObject;

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        Hide();
    }

    public void Show()
    {
        if (root == null)
        {
            Debug.LogError("SIM_ScreenController.Show failed: root is null.");
            return;
        }

        root.SetActive(true);
    }

    public void Hide()
    {
        if (root == null)
            return;

        root.SetActive(false);
    }

    public void SetMessage(string text)
    {
        if (messageText == null)
        {
            Debug.LogError("SIM_ScreenController.SetMessage failed: messageText is null.");
            return;
        }

        messageText.SetText(text);
    }

    public void ShowMessage(string text)
    {
        if (lastMessageOnlyText == text && root != null && root.activeSelf)
            return;

        lastMessageOnlyText = text;
        lastMenuSignature = null;

        SetMessage(text);
        Show();
    }

    public void DestroyAllButtons()
    {
        for (int i = 0; i < activeButtons.Count; i++)
        {
            if (activeButtons[i] != null)
                Destroy(activeButtons[i].gameObject);
        }

        activeButtons.Clear();
    }

    public SIM_Button AddButton(string label, Action onClick)
    {
        if (buttonsParent == null)
        {
            Debug.LogError("SIM_ScreenController.AddButton failed: buttonsParent is null.");
            return null;
        }

        SIM_Button newButton = SIM_Button.Create(buttonsParent, label, onClick);

        if (newButton != null)
            activeButtons.Add(newButton);

        return newButton;
    }

    public void ShowMenu(string message, params ButtonDefinition[] buttons)
    {
        ShowMenuInternal(message, false, buttons);
    }

    public void ShowMenuForced(string message, params ButtonDefinition[] buttons)
    {
        ShowMenuInternal(message, true, buttons);
    }

    private void ShowMenuInternal(string message, bool forceRebuild, params ButtonDefinition[] buttons)
    {
        string signature = BuildMenuSignature(message, buttons);

        bool alreadyShowingSameMenu =
            !forceRebuild &&
            lastMenuSignature == signature &&
            root != null &&
            root.activeSelf &&
            activeButtons.Count == buttons.Length;

        if (alreadyShowingSameMenu)
            return;

        lastMenuSignature = signature;
        lastMessageOnlyText = null;

        Show();
        SetMessage(message);
        DestroyAllButtons();

        for (int i = 0; i < buttons.Length; i++)
            AddButton(buttons[i].Label, buttons[i].OnClick);
    }

    public void ClearAndHide()
    {
        DestroyAllButtons();
        SetMessage(string.Empty);
        Hide();

        lastMenuSignature = null;
        lastMessageOnlyText = null;
    }

    private string BuildMenuSignature(string message, ButtonDefinition[] buttons)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(message);

        for (int i = 0; i < buttons.Length; i++)
        {
            sb.Append("|");
            sb.Append(buttons[i].Label);
        }

        return sb.ToString();
    }

    [Serializable]
    public struct ButtonDefinition
    {
        public string Label;
        public Action OnClick;

        public ButtonDefinition(string label, Action onClick)
        {
            Label = label;
            OnClick = onClick;
        }
    }
}
