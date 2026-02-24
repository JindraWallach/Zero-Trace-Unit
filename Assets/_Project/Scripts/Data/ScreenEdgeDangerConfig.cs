using UnityEngine;

/// <summary>
/// ScriptableObject configuration for screen edge danger vignette.
/// Controls how red border intensity maps to camera suspicion (0–100%).
/// Create via: Assets > Create > Zero Trace > Screen Edge Danger Config
/// </summary>
[CreateAssetMenu(fileName = "ScreenEdgeDangerConfig", menuName = "Zero Trace/Screen Edge Danger Config")]
public class ScreenEdgeDangerConfig : ScriptableObject
{
    [Header("Suspicion Thresholds (0–100)")]
    [Tooltip("Suspicion % at which border first becomes visible (maps to pixel multiplier ~1.2)")]
    [Range(0f, 100f)]
    public float visibleThreshold = 20f;

    [Tooltip("Suspicion % at which border reaches full intensity (maps to pixel multiplier ~1.88)")]
    [Range(0f, 100f)]
    public float maxIntensityThreshold = 100f;

    [Header("Visual Settings")]
    [Tooltip("Border color (should stay red; alpha is driven by suspicion)")]
    public Color borderColor = new Color(1f, 0f, 0f, 1f);

    [Tooltip("Minimum border alpha when just becoming visible")]
    [Range(0f, 1f)]
    public float minAlpha = 0.15f;

    [Tooltip("Maximum border alpha at full detection")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.85f;

    [Tooltip("Lerp speed toward target alpha (higher = faster, e.g. 5 = smooth, 20 = snappy)")]
    [Range(0.5f, 30f)]
    public float lerpSpeed = 8f;

    [Header("Image pixelsPerUnitMultiplier Range")]
    [Tooltip("pixelsPerUnitMultiplier at minimum intensity (border just visible)")]
    [Range(0f, 10f)]
    public float minPixelsPerUnit = 0.5f;

    [Tooltip("pixelsPerUnitMultiplier at maximum intensity (full red border)")]
    [Range(0f, 10f)]
    public float maxPixelsPerUnit = 1.5f;

    private void OnValidate()
    {
        if (maxIntensityThreshold <= visibleThreshold)
            Debug.LogWarning("[ScreenEdgeDangerConfig] maxIntensityThreshold must be greater than visibleThreshold.");

        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp(maxAlpha, minAlpha, 1f);
        minPixelsPerUnit = Mathf.Max(0f, minPixelsPerUnit);
        maxPixelsPerUnit = Mathf.Max(minPixelsPerUnit, maxPixelsPerUnit);
    }

    /// <summary>
    /// Returns normalized danger intensity (0–1) based on raw suspicion value (0–100).
    /// Returns 0 below visibleThreshold, 1 at or above maxIntensityThreshold.
    /// </summary>
    public float GetNormalizedIntensity(float suspicion)
    {
        if (suspicion <= visibleThreshold) return 0f;
        if (suspicion >= maxIntensityThreshold) return 1f;
        return (suspicion - visibleThreshold) / (maxIntensityThreshold - visibleThreshold);
    }

    /// <summary>
    /// Returns target alpha for the border image based on suspicion.
    /// 0 below visibleThreshold, lerps minAlpha→maxAlpha up to maxIntensityThreshold.
    /// </summary>
    public float GetTargetAlpha(float suspicion)
    {
        float t = GetNormalizedIntensity(suspicion);
        if (t <= 0f) return 0f;
        return Mathf.Lerp(minAlpha, maxAlpha, t);
    }

    /// <summary>
    /// Returns border pixel width based on suspicion.
    /// </summary>
    public float GetBorderPixels(float suspicion)
    {
        float t = GetNormalizedIntensity(suspicion);
        return Mathf.Lerp(minPixelsPerUnit, maxPixelsPerUnit, t);
    }
}