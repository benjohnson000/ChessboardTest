using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// UNITY ONLY!!!
/// Simulates a button that can be shown on the screen popup and bound to an action.
/// </summary>
public class SIM_Button : MonoBehaviour
{
    [SerializeField] private Button button;

    [FormerlySerializedAs("text")]
    [SerializeField] private TextMeshProUGUI label;

    public static SIM_Button Create(Transform parent, string labelText, Action onClick = null)
    {
        SIM_Button prefab = Resources.Load<SIM_Button>("Button");

        if (prefab == null)
        {
            Debug.LogError("SIM_Button.Create failed: could not load Resources/Button prefab.");
            return null;
        }

        if (parent == null)
        {
            Debug.LogError("SIM_Button.Create failed: parent is null.");
            return null;
        }

        SIM_Button simButton = Instantiate(prefab, parent, false);

        RectTransform rect = simButton.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
        }

        simButton.Initialize(labelText, onClick);
        return simButton;
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Initialize(string labelText, Action onClick = null)
    {
        if (button == null)
            button = GetComponent<Button>();

        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>(true);

        if (button == null)
        {
            Debug.LogError("SIM_Button.Initialize failed: Button component missing.");
            return;
        }

        if (label == null)
        {
            Debug.LogError("SIM_Button.Initialize failed: TextMeshProUGUI label missing.");
            return;
        }

        SetLabel(labelText);
        ClearListeners();

        if (onClick != null)
            button.onClick.AddListener(() => onClick.Invoke());
    }

    public void SetLabel(string text)
    {
        if (label == null)
        {
            Debug.LogError("SIM_Button.SetLabel failed: label is null.");
            return;
        }

        label.SetText(text);
    }

    public void AddListener(Action onClick)
    {
        if (onClick == null || button == null)
            return;

        button.onClick.AddListener(() => onClick.Invoke());
    }

    public void ClearListeners()
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
