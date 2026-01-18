using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for a single node in packet tracing puzzle.
/// Handles click events and visual states.
/// </summary>
[RequireComponent(typeof(Button))]
public class NodeUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image nodeImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image selectionRing; // Optional: border when selected

    private Button button;
    private int index;
    private NodeType type;
    private Color baseColor;
    private Action<NodeUI> clickCallback;

    public int Index => index;
    public NodeType Type => type;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    public void Setup(int index, NodeType type, Color color, Action<NodeUI> clickCallback)
    {
        this.index = index;
        this.type = type;
        this.baseColor = color;
        this.clickCallback = clickCallback;

        if (nodeImage != null)
            nodeImage.color = color;

        if (labelText != null)
        {
            switch (type)
            {
                case NodeType.Start:
                    labelText.text = "START";
                    break;
                case NodeType.Goal:
                    labelText.text = "GOAL";
                    break;
                case NodeType.Disabled:
                    labelText.text = "X";
                    break;
                default:
                    labelText.text = index.ToString();
                    break;
            }
        }

        // Disable button for disabled nodes
        if (button != null)
            button.interactable = type != NodeType.Disabled;

        if (selectionRing != null)
            selectionRing.gameObject.SetActive(false);
    }

    private void OnClicked()
    {
        clickCallback?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectionRing != null)
            selectionRing.gameObject.SetActive(selected);
    }

    public void SetConnected(bool connected, Color connectedColor)
    {
        if (nodeImage != null)
        {
            // Don't override START/GOAL colors completely, just brighten them
            if (type == NodeType.Start || type == NodeType.Goal)
            {
                nodeImage.color = connected ? Color.Lerp(baseColor, connectedColor, 0.3f) : baseColor;
            }
            else
            {
                nodeImage.color = connected ? connectedColor : baseColor;
            }
        }
    }
}

public enum NodeType
{
    Normal,
    Start,
    Goal,
    Disabled
}