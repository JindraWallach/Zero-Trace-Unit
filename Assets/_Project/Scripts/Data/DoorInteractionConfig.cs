using UnityEngine;

/// <summary>
/// Configuration for door interaction ranges, prompts, colors, and input keys.
/// Single source of truth for all door UI/interaction settings.
/// </summary>
[CreateAssetMenu(fileName = "DoorInteractionConfig", menuName = "Zero Trace/Door Interaction Config")]
public class DoorInteractionConfig : ScriptableObject
{
    [Header("Interaction Ranges")]
    [Tooltip("Physical interaction range in Normal mode (meters)")]
    public float physicalInteractionRange = 6f;

    [Tooltip("Hack interaction range in Hack mode (meters)")]
    public float hackRange = 15f;

    [Header("UI Prompts - Text")]
    [Tooltip("Text for opening door (both modes, unlocked, closed)")]
    public string physicalUseText = "Open";

    [Tooltip("Text for closing door (both modes, unlocked, open)")]
    public string physicalCloseText = "Close";

    [Tooltip("Text for hack interaction (Hack mode, locked)")]
    public string hackText = "HACK";

    [Tooltip("Text for locked state (normal mode, locked)")]
    public string lockedText = "Locked";

    [Tooltip("Text for out of range (Hack mode only)")]
    public string outOfRangeText = "OUT OF RANGE";

    [Header("UI Prompts - Colors")]
    [Tooltip("Color for physical interactions (green for open/close)")]
    public Color physicalColor = new Color(0.2f, 1f, 0.2f); // Green

    [Tooltip("Color for hack interactions (yellow/orange)")]
    public Color hackColor = new Color(1f, 0.8f, 0f); // Yellow/Orange

    [Tooltip("Color for locked/unavailable state (red)")]
    public Color lockedColor = new Color(1f, 0.2f, 0.2f); // Red

    [Header("Input Key Display")]
    [Tooltip("Key to show for interaction (e.g. 'E', 'F', 'X')")]
    public string interactKey = "E";

    [Tooltip("Format for key display: {0} = key, {1} = action text")]
    public string keyDisplayFormat = "[{0}] {1}";

    [Tooltip("Show key in prompt (if false, only shows action text)")]
    public bool showKeyInPrompt = true;

    private void OnValidate()
    {
        physicalInteractionRange = Mathf.Max(0.1f, physicalInteractionRange);
        hackRange = Mathf.Max(0.1f, hackRange);

        if (hackRange < physicalInteractionRange)
            Debug.LogWarning($"[DoorInteractionConfig] Hack range ({hackRange}m) < physical range ({physicalInteractionRange}m)");

        // Ensure key is not empty
        if (string.IsNullOrWhiteSpace(interactKey))
            interactKey = "E";
    }

    /// <summary>
    /// Formats prompt text with key (if enabled).
    /// Example: "[E] Open" or just "Open"
    /// </summary>
    public string FormatPrompt(string actionText)
    {
        if (!showKeyInPrompt || string.IsNullOrEmpty(actionText))
            return actionText;

        return string.Format(keyDisplayFormat, interactKey, actionText);
    }
}