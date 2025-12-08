using UnityEngine;

/// <summary>
/// ScriptableObject configuration for enemy suspicion system.
/// Separate from EnemyConfig for modularity (SRP).
/// Create via: Assets > Create > Zero Trace > Suspicion Config
/// </summary>
[CreateAssetMenu(fileName = "SuspicionConfig", menuName = "Zero Trace/Suspicion Config")]
public class SuspicionConfig : ScriptableObject
{
    [Header("Build & Decay Rates")]
    [Tooltip("Base suspicion increase per second when player visible (1 body part)")]
    [Range(5f, 50f)]
    public float baseBuildRate = 15f;

    [Tooltip("Suspicion decrease per second when player hidden")]
    [Range(5f, 30f)]
    public float decayRate = 10f;

    [Tooltip("Delay before suspicion starts decaying after losing sight")]
    [Range(0f, 3f)]
    public float decayGracePeriod = 1f;

    [Header("Visibility Multiplier (Exponential)")]
    [Tooltip("Use exponential formula: 1 part = 1x, 2 = 2x, 3 = 4x, 4 = 8x")]
    public bool useExponentialMultiplier = true;

    [Tooltip("Manual multipliers if not using exponential (4 values for 1-4 parts)")]
    public float[] manualMultipliers = new float[] { 1f, 1.5f, 2.5f, 4f };

    [Header("Thresholds")]
    [Tooltip("Suspicion % to trigger Alert state")]
    [Range(10f, 99f)]
    public float alertThreshold = 30f;

    [Tooltip("Suspicion % to trigger Chase state (always 100)")]
    public float chaseThreshold = 100f;

    [Header("Performance")]
    [Tooltip("Update interval for suspicion calculations (seconds)")]
    [Range(0.05f, 0.3f)]
    public float updateInterval = 0.1f;

    [Tooltip("Vision check interval for multi-point raycasts (seconds)")]
    [Range(0.1f, 0.5f)]
    public float visionCheckInterval = 0.2f;

    [Header("Debug")]
    [Tooltip("Show suspicion bar gizmo in Scene view")]
    public bool showDebugBar = true;

    [Tooltip("Show suspicion value in bar label")]
    public bool showSuspicionValue = true;

    private void OnValidate()
    {
        // Clamp values
        baseBuildRate = Mathf.Max(1f, baseBuildRate);
        decayRate = Mathf.Max(1f, decayRate);
        alertThreshold = Mathf.Clamp(alertThreshold, 1f, 99f);
        chaseThreshold = 100f; // Always 100

        // Validate manual multipliers
        if (manualMultipliers == null || manualMultipliers.Length != 4)
        {
            manualMultipliers = new float[] { 1f, 1.5f, 2.5f, 4f };
        }

        // Ensure multipliers are ascending
        for (int i = 0; i < manualMultipliers.Length; i++)
        {
            manualMultipliers[i] = Mathf.Max(1f, manualMultipliers[i]);
        }
    }

    /// <summary>
    /// Get visibility multiplier based on number of visible body parts (1-4).
    /// </summary>
    public float GetVisibilityMultiplier(int visibleParts)
    {
        visibleParts = Mathf.Clamp(visibleParts, 0, 4);

        if (visibleParts == 0)
            return 0f;

        if (useExponentialMultiplier)
        {
            // Exponential: 1x, 2x, 4x, 8x
            return Mathf.Pow(2f, visibleParts - 1);
        }
        else
        {
            // Manual: use array
            return manualMultipliers[visibleParts - 1];
        }
    }

    /// <summary>
    /// Calculate effective build rate based on visible parts.
    /// </summary>
    public float GetEffectiveBuildRate(int visibleParts)
    {
        return baseBuildRate * GetVisibilityMultiplier(visibleParts);
    }
}