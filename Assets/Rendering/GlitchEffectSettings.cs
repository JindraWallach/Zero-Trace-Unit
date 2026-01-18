using UnityEngine;

/// <summary>
/// ScriptableObject for glitch effect configuration.
/// Allows sharing settings across multiple scenes and easy runtime tweaking.
/// </summary>
[CreateAssetMenu(fileName = "GlitchEffectSettings", menuName = "Rendering/Glitch Effect Settings")]
public class GlitchEffectSettings : ScriptableObject
{
    [Header("Base Settings")]
    [Range(0f, 0.15f)]
    [Tooltip("Overall glitch strength. 0 = disabled, 0.15 = extreme")]
    public float intensity = 0.02f;

    [Range(0f, 20f)]
    [Tooltip("Speed multiplier for glitch animation. Use 0 for static glitch")]
    public float timeScale = 1f;

    [Header("Visual Parameters")]
    [Range(0f, 0.1f)]
    [Tooltip("RGB color separation amount (chromatic aberration)")]
    public float colorShift = 0.01f;

    [Range(1f, 200f)]
    [Tooltip("Size of glitch blocks. Lower = smaller blocks, more detail")]
    public float blockSize = 20f;

    [Range(0f, 1f)]
    [Tooltip("Probability of scanline distortion")]
    public float scanlineIntensity = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Probability of color inversion glitches")]
    public float inversionIntensity = 0.5f;

    [Header("Advanced")]
    [Range(0f, 0.2f)]
    [Tooltip("Vertical displacement strength")]
    public float verticalShift = 0f;

    [Range(0f, 10f)]
    [Tooltip("Noise frequency multiplier")]
    public float noiseFrequency = 1f;

    [Tooltip("Enable/disable the effect entirely")]
    public bool enabled = true;

    [Header("Performance")]
    [Tooltip("Skip every Nth frame for performance (1 = no skip, 2 = every other frame)")]
    [Range(1, 4)]
    public int updateFrequency = 1;

    // Preset configurations
    [Header("Quick Presets")]
    [Tooltip("Select preset to apply values. Manual changes set to Custom automatically.")]
    [SerializeField] private GlitchPreset currentPreset = GlitchPreset.Custom;

    // Track if user manually changed values
    private GlitchPreset lastAppliedPreset = GlitchPreset.Custom;
    private bool isApplyingPreset = false;

    public enum GlitchPreset
    {
        Custom,
        None,
        Minimal,      // Velmi jemný
        VerySubtle,   // Skoro neviditelný
        Subtle,       // Jemný
        Medium,       // Střední
        Extreme,      // Silný
        Static,       // Frozen
        Death         // Death sequence
    }

    public GlitchPreset CurrentPreset
    {
        get => currentPreset;
        set
        {
            if (currentPreset != value && value != GlitchPreset.Custom)
            {
                currentPreset = value;
                ApplyPreset(value);
            }
        }
    }

    /// <summary>
    /// Apply preset values without switching back to Custom.
    /// </summary>
    public void ApplyPreset(GlitchPreset preset)
    {
        // Prevent OnValidate from triggering during preset application
        isApplyingPreset = true;
        lastAppliedPreset = preset;
        currentPreset = preset;

        switch (preset)
        {
            case GlitchPreset.None:
                intensity = 0f;
                timeScale = 0f;
                colorShift = 0f;
                blockSize = 20f;
                scanlineIntensity = 0f;
                inversionIntensity = 0f;
                verticalShift = 0f;
                noiseFrequency = 0f;
                enabled = false;
                break;
            case GlitchPreset.Minimal:
                intensity = 0.003f;
                timeScale = 0.2f;
                colorShift = 0.001f;
                blockSize = 0f;
                scanlineIntensity = 0.1f;
                inversionIntensity = 0f;
                verticalShift = 0f;
                noiseFrequency = 0.5f;
                break;

            case GlitchPreset.VerySubtle:
                intensity = 0.006f;
                timeScale = 0.3f;
                colorShift = 0.002f;
                blockSize = 0f;
                scanlineIntensity = 0.2f;
                inversionIntensity = 0.05f;
                verticalShift = 0f;
                noiseFrequency = 0.8f;
                break;

            case GlitchPreset.Subtle:
                intensity = 0.01f;
                timeScale = 0.5f;
                colorShift = 0.003f;
                blockSize = 0f;
                scanlineIntensity = 0.3f;
                inversionIntensity = 0.1f;
                verticalShift = 0f;
                noiseFrequency = 1f;
                break;

            case GlitchPreset.Medium:
                intensity = 0.03f;
                timeScale = 1f;
                colorShift = 0.008f;
                blockSize = 0f;
                scanlineIntensity = 0.5f;
                inversionIntensity = 0.3f;
                verticalShift = 0.01f;
                noiseFrequency = 1f;
                break;

            case GlitchPreset.Extreme:
                intensity = 0.08f;
                timeScale = 3f;
                colorShift = 0.02f;
                blockSize = 0f;
                scanlineIntensity = 0.8f;
                inversionIntensity = 0.6f;
                verticalShift = 0.05f;
                noiseFrequency = 2f;
                break;

            case GlitchPreset.Static:
                intensity = 0.02f;
                timeScale = 0f; // Frozen glitch
                colorShift = 0.005f;
                blockSize = 0f;
                scanlineIntensity = 0.4f;
                inversionIntensity = 0.2f;
                verticalShift = 0f;
                noiseFrequency = 1f;
                break;

            case GlitchPreset.Death:
                intensity = 0.1f;
                timeScale = 2f;
                colorShift = 0.05f; // Jen Death má velký colorShift
                blockSize = 0f;
                scanlineIntensity = 0.9f;
                inversionIntensity = 0.8f;
                verticalShift = 0.08f;
                noiseFrequency = 3f;
                break;

            case GlitchPreset.Custom:
                // Don't change values for Custom preset
                break;
        }

        isApplyingPreset = false;

#if UNITY_EDITOR
        // Mark asset as dirty to save changes
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Check if current values match the selected preset.
    /// If not, switch to Custom.
    /// </summary>
    private bool ValuesMatchPreset(GlitchPreset preset)
    {
        // Custom always matches
        if (preset == GlitchPreset.Custom)
            return true;

        // Create temp settings to compare
        var temp = CreateInstance<GlitchEffectSettings>();
        temp.ApplyPreset(preset);

        const float tolerance = 0.001f;

        bool matches =
            Mathf.Abs(intensity - temp.intensity) < tolerance &&
            Mathf.Abs(timeScale - temp.timeScale) < tolerance &&
            Mathf.Abs(colorShift - temp.colorShift) < tolerance &&
            Mathf.Abs(blockSize - temp.blockSize) < tolerance &&
            Mathf.Abs(scanlineIntensity - temp.scanlineIntensity) < tolerance &&
            Mathf.Abs(inversionIntensity - temp.inversionIntensity) < tolerance &&
            Mathf.Abs(verticalShift - temp.verticalShift) < tolerance &&
            Mathf.Abs(noiseFrequency - temp.noiseFrequency) < tolerance;

        DestroyImmediate(temp);
        return matches;
    }

    private void OnValidate()
    {
        // Skip validation during preset application
        if (isApplyingPreset)
            return;

        // If user changed preset dropdown, apply it
        if (currentPreset != lastAppliedPreset && currentPreset != GlitchPreset.Custom)
        {
            ApplyPreset(currentPreset);
            return;
        }

        // If user manually changed values, check if they still match preset
        if (currentPreset != GlitchPreset.Custom)
        {
            if (!ValuesMatchPreset(currentPreset))
            {
                // Values changed manually, switch to Custom
                currentPreset = GlitchPreset.Custom;
                lastAppliedPreset = GlitchPreset.Custom;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }

    /// <summary>
    /// Editor utility: Apply preset via context menu.
    /// </summary>
    /// 
    [ContextMenu("Apply None Preset")]
    private void ApplyNone() => ApplyPreset(GlitchPreset.None);

    [ContextMenu("Apply Minimal Preset")]
    private void ApplyMinimal() => ApplyPreset(GlitchPreset.Minimal);

    [ContextMenu("Apply Very Subtle Preset")]
    private void ApplyVerySubtle() => ApplyPreset(GlitchPreset.VerySubtle);

    [ContextMenu("Apply Subtle Preset")]
    private void ApplySubtle() => ApplyPreset(GlitchPreset.Subtle);

    [ContextMenu("Apply Medium Preset")]
    private void ApplyMedium() => ApplyPreset(GlitchPreset.Medium);

    [ContextMenu("Apply Extreme Preset")]
    private void ApplyExtreme() => ApplyPreset(GlitchPreset.Extreme);

    [ContextMenu("Apply Static Preset")]
    private void ApplyStatic() => ApplyPreset(GlitchPreset.Static);

    [ContextMenu("Apply Death Preset")]
    private void ApplyDeath() => ApplyPreset(GlitchPreset.Death);

    [ContextMenu("Reset to Custom")]
    private void ResetToCustom()
    {
        currentPreset = GlitchPreset.Custom;
        lastAppliedPreset = GlitchPreset.Custom;
    }
}