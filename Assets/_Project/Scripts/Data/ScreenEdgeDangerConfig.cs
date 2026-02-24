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

    [Tooltip("Duration (seconds) for alpha to interpolate to target value")]
    [Range(0f, 1f)]
    public float lerpSpeed = 0.15f;

    [Header("Image (Sliced Sprite) Settings")]
    [Tooltip("Border width in pixels at minimum intensity (suspicion ~1.2 camera multiplier)")]
    [Range(0f, 120f)]
    public float minBorderPixels = 20f;

    [Tooltip("Border width in pixels at maximum intensity (suspicion ~1.88 camera multiplier)")]
    [Range(0f, 300f)]
    public float maxBorderPixels = 80f;

    private void OnValidate()
    {
        if (maxIntensityThreshold <= visibleThreshold)
            Debug.LogWarning("[ScreenEdgeDangerConfig] maxIntensityThreshold must be greater than visibleThreshold.");

        minAlpha = Mathf.Clamp01(minAlpha);
        maxAlpha = Mathf.Clamp(maxAlpha, minAlpha, 1f);
        minBorderPixels = Mathf.Max(0f, minBorderPixels);
        maxBorderPixels = Mathf.Max(minBorderPixels, maxBorderPixels);
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
    /// </summary>
    public float GetTargetAlpha(float suspicion)
    {
        float t = GetNormalizedIntensity(suspicion);
        return Mathf.Lerp(0f, Mathf.Lerp(minAlpha, maxAlpha, t), Mathf.Ceil(t));
    }

    /// <summary>
    /// Returns border pixel width based on suspicion.
    /// </summary>
    public float GetBorderPixels(float suspicion)
    {
        float t = GetNormalizedIntensity(suspicion);
        return Mathf.Lerp(minBorderPixels, maxBorderPixels, t);
    }
}