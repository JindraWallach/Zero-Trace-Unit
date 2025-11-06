using UnityEngine;

/// <summary>
/// Configuration for door interaction ranges and prompts.
/// CREATE ASSET: Right-click in Project → Create → Zero Trace → Door Interaction Config
/// </summary>
[CreateAssetMenu(fileName = "DoorInteractionConfig", menuName = "Zero Trace/Door Interaction Config")]
public class DoorInteractionConfig : ScriptableObject
{
    [Header("Interaction Ranges")]
    [Tooltip("Physical interaction range in Normal mode (meters)")]
    public float physicalInteractionRange = 6f; // Increased to match trigger collider

    [Tooltip("Hack interaction range in Hack mode (meters)")]
    public float hackRange = 15f;

    [Header("UI Prompts")]
    [Tooltip("Text for physical interaction (Normal mode, unlocked)")]
    public string physicalUseText = "Open";

    [Tooltip("Text for hack interaction (Hack mode, locked)")]
    public string hackText = "HACK";

    [Tooltip("Text for out of range (Hack mode only)")]
    public string outOfRangeText = "OUT OF RANGE";

    private void OnValidate()
    {
        // Ensure ranges are positive
        physicalInteractionRange = Mathf.Max(0.1f, physicalInteractionRange);
        hackRange = Mathf.Max(0.1f, hackRange);

        // Warn if hack range is too small
        if (hackRange < physicalInteractionRange)
        {
            Debug.LogWarning($"[DoorInteractionConfig] Hack range ({hackRange}m) is less than physical range ({physicalInteractionRange}m). This may cause issues!");
        }
    }
}