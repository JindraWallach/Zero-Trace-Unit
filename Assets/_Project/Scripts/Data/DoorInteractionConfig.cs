using UnityEngine;

/// <summary>
/// Configuration for door interaction ranges and prompts.
/// Single source of truth for all interaction settings.
/// </summary>
[CreateAssetMenu(fileName = "DoorInteractionConfig", menuName = "Zero Trace/Door Interaction Config")]
public class DoorInteractionConfig : ScriptableObject
{
    [Header("Interaction Ranges")]
    [Tooltip("Physical interaction range in Normal mode (meters)")]
    public float physicalInteractionRange = 6f;

    [Tooltip("Hack interaction range in Hack mode (meters)")]
    public float hackRange = 15f;

    [Header("UI Prompts")]
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

    private void OnValidate()
    {
        physicalInteractionRange = Mathf.Max(0.1f, physicalInteractionRange);
        hackRange = Mathf.Max(0.1f, hackRange);

        if (hackRange < physicalInteractionRange)
            Debug.LogWarning($"[DoorInteractionConfig] Hack range ({hackRange}m) < physical range ({physicalInteractionRange}m)");
    }
}