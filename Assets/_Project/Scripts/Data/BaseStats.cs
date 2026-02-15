using UnityEngine;

/// <summary>
/// Base stats container - stores ORIGINAL unmultiplied values.
/// These values NEVER change at runtime.
/// Single Responsibility: Data storage for base stats.
/// </summary>
[CreateAssetMenu(fileName = "BaseStats", menuName = "Zero Trace/Stats/Base Stats")]
public class BaseStats : ScriptableObject
{
    [Header("Original Values - DO NOT MODIFY AT RUNTIME")]
    [Tooltip("Original vision range before any multipliers")]
    public float originalVisionRange = 20f;

    [Tooltip("Original patrol speed before any multipliers")]
    public float originalPatrolSpeed = 1.5f;

    [Tooltip("Original chase speed before any multipliers")]
    public float originalChaseSpeed = 4f;

    [Tooltip("Original search speed before any multipliers")]
    public float originalSearchSpeed = 2f;

    [Header("Player Stats")]
    [Tooltip("Original player movement speed")]
    public float originalPlayerSpeed = 5f;

    [Tooltip("Original player sprint speed")]
    public float originalPlayerSprintSpeed = 8f;

    // Add more base stats as needed

    /// <summary>
    /// Reset to design-time values (Editor only).
    /// Call this manually if you need to reset SO values.
    /// </summary>
    public void ResetToDefaults()
    {
#if UNITY_EDITOR
        originalVisionRange = 20f;
        originalPatrolSpeed = 1.5f;
        originalChaseSpeed = 4f;
        originalSearchSpeed = 2f;
        originalPlayerSpeed = 5f;
        originalPlayerSprintSpeed = 8f;

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[BaseStats] Reset to defaults: {name}");
#endif
    }
}