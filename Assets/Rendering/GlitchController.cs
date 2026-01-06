using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Optimized runtime controller for screen glitch effect.
/// Uses ScriptableObject for settings and supports preset transitions.
/// SRP: Single responsibility - manages glitch effect state and animations only.
/// </summary>
public class GlitchController : MonoBehaviour
{
    public static GlitchController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GlitchEffectSettings glitchSettings;

    [Header("Renderer Setup")]
    [SerializeField] private UniversalRendererData rendererData;

    [Header("Death Glitch Config")]
    [SerializeField] private float deathGlitchDuration = 2f;
    [SerializeField] private AnimationCurve deathGlitchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private GlitchEffectSettings.GlitchPreset deathPreset = GlitchEffectSettings.GlitchPreset.Death;

    [Header("Pulse Glitch Config")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float pulseIntensityMultiplier = 2f;

    [Header("Transition Config")]
    [SerializeField] private float transitionSpeed = 2f;

    private ScreenGlitchFeature glitchFeature;
    private Coroutine activeGlitchCoroutine;
    private Coroutine activeTransitionCoroutine;

    // Cached values for optimization
    private float baseIntensity;
    private float baseTimeScale;
    private bool isInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindGlitchFeature();
        SubscribeToEvents();

        if (glitchSettings != null)
        {
            CacheBaseValues();
            isInitialized = true;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();

        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (glitchSettings != null)
            glitchSettings.enabled = false;
    }

    // === SETUP ===

    private void FindGlitchFeature()
    {
        if (rendererData == null)
        {
            Debug.LogWarning("[GlitchController] Please assign UniversalRendererData in inspector!");
            return;
        }

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is ScreenGlitchFeature screenGlitch)
            {
                glitchFeature = screenGlitch;

                // Use feature's settings if not assigned
                if (glitchSettings == null)
                {
                    glitchSettings = screenGlitch.SettingsAsset;
                }

                Debug.Log("[GlitchController] Found ScreenGlitchFeature");
                return;
            }
        }

        Debug.LogWarning("[GlitchController] ScreenGlitchFeature not found in renderer!");
    }

    private void CacheBaseValues()
    {
        if (glitchSettings == null) return;

        baseIntensity = glitchSettings.intensity;
        baseTimeScale = glitchSettings.timeScale;
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied += TriggerDeathGlitch;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        else
        {
            Debug.LogWarning("[GlitchController] GameManager instance not found!");
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied -= TriggerDeathGlitch;
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    // === EVENT HANDLERS ===

    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.InPuzzle:
                TriggerPulseGlitch();
                break;
            case GameState.Dead:
                // Handled by OnPlayerDied
                break;
            case GameState.Playing:
                // Restore base settings
                if (isInitialized)
                    TransitionToPreset(GlitchEffectSettings.GlitchPreset.Subtle, 0.5f);
                break;
        }
    }

    // === PUBLIC API (Simple Enable/Disable) ===

    /// <summary>
    /// Enable glitch effect (sets enabled flag in SO).
    /// </summary>
    public void EnableGlitch()
    {
        if (glitchSettings == null)
        {
            Debug.LogWarning("[GlitchController] Glitch settings not assigned!");
            return;
        }

        glitchSettings.enabled = true;
        Debug.Log("[GlitchController] Glitch enabled");
    }

    /// <summary>
    /// Disable glitch effect (sets enabled flag in SO).
    /// </summary>
    public void DisableGlitch()
    {
        if (glitchSettings == null)
        {
            Debug.LogWarning("[GlitchController] Glitch settings not assigned!");
            return;
        }

        glitchSettings.enabled = false;
        Debug.Log("[GlitchController] Glitch disabled");
    }

    /// <summary>
    /// Toggle glitch effect on/off.
    /// </summary>
    public void ToggleGlitch()
    {
        if (glitchSettings == null) return;

        if (glitchSettings.enabled)
            DisableGlitch();
        else
            EnableGlitch();
    }

    /// <summary>
    /// Check if glitch is currently enabled.
    /// </summary>
    public bool IsGlitchEnabled()
    {
        return glitchSettings != null && glitchSettings.enabled;
    }

    // === DEATH SEQUENCE ===

    /// <summary>
    /// Trigger death glitch sequence with preset.
    /// </summary>
    public void TriggerDeathGlitch()
    {
        if (glitchSettings == null)
        {
            Debug.LogWarning("[GlitchController] Glitch settings not assigned!");
            return;
        }

        if (activeGlitchCoroutine != null)
            StopCoroutine(activeGlitchCoroutine);

        activeGlitchCoroutine = StartCoroutine(DeathGlitchCoroutine());
    }

    // === PULSE EFFECT ===

    /// <summary>
    /// Trigger short pulse glitch (intensity spike).
    /// </summary>
    public void TriggerPulseGlitch()
    {
        if (glitchSettings == null)
        {
            Debug.LogWarning("[GlitchController] Glitch settings not assigned!");
            return;
        }

        // Don't pulse if disabled
        if (!glitchSettings.enabled)
            return;

        StartCoroutine(PulseGlitchCoroutine());
    }

    // === PRESET TRANSITIONS ===

    /// <summary>
    /// Smoothly transition to a preset over time.
    /// </summary>
    public void TransitionToPreset(GlitchEffectSettings.GlitchPreset preset, float duration = 1f)
    {
        if (glitchSettings == null) return;

        if (activeTransitionCoroutine != null)
            StopCoroutine(activeTransitionCoroutine);

        activeTransitionCoroutine = StartCoroutine(TransitionToPresetCoroutine(preset, duration));
    }

    // === DIRECT SETTERS (For runtime tweaking) ===

    /// <summary>
    /// Set intensity directly (0-1 range).
    /// </summary>
    public void SetIntensity(float intensity)
    {
        if (glitchSettings == null) return;
        glitchSettings.intensity = Mathf.Clamp(intensity, 0f, 0.15f);
    }

    /// <summary>
    /// Set enabled state directly.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (glitchSettings == null) return;
        glitchSettings.enabled = enabled;
    }

    /// <summary>
    /// Freeze glitch animation (timeScale = 0).
    /// </summary>
    public void FreezeGlitch(bool freeze)
    {
        if (glitchSettings == null) return;
        glitchSettings.timeScale = freeze ? 0f : baseTimeScale;
    }

    /// <summary>
    /// Reset to cached base values.
    /// </summary>
    public void ResetToBase()
    {
        if (glitchSettings == null) return;

        glitchSettings.intensity = baseIntensity;
        glitchSettings.timeScale = baseTimeScale;
    }

    // === COROUTINES ===

    private IEnumerator DeathGlitchCoroutine()
    {
        float elapsed = 0f;

        // Store start values
        float startIntensity = glitchSettings.intensity;
        bool wasEnabled = glitchSettings.enabled;

        // Enable and apply death preset
        glitchSettings.enabled = true;
        glitchSettings.ApplyPreset(deathPreset);

        while (elapsed < deathGlitchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / deathGlitchDuration;

            // Animate intensity using curve
            float curveValue = deathGlitchCurve.Evaluate(t);
            glitchSettings.intensity = Mathf.Lerp(startIntensity, 0.15f, curveValue);

            yield return null;
        }

        // Reset to base
        ResetToBase();
        glitchSettings.enabled = wasEnabled; // Restore original state

        activeGlitchCoroutine = null;
    }

    private IEnumerator PulseGlitchCoroutine()
    {
        float elapsed = 0f;
        float startIntensity = glitchSettings.intensity;
        float targetIntensity = Mathf.Min(startIntensity * pulseIntensityMultiplier, 0.15f);

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;

            // Sine wave pulse: 0 -> 1 -> 0
            float pulse = Mathf.Sin(t * Mathf.PI);
            glitchSettings.intensity = Mathf.Lerp(startIntensity, targetIntensity, pulse);

            yield return null;
        }

        glitchSettings.intensity = startIntensity;
    }

    private IEnumerator TransitionToPresetCoroutine(GlitchEffectSettings.GlitchPreset targetPreset, float duration)
    {
        // Create temporary settings to get target values
        var tempSettings = ScriptableObject.CreateInstance<GlitchEffectSettings>();
        tempSettings.ApplyPreset(targetPreset);

        // Store start values
        float startIntensity = glitchSettings.intensity;
        float startTimeScale = glitchSettings.timeScale;
        float startColorShift = glitchSettings.colorShift;
        float startBlockSize = glitchSettings.blockSize;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Smooth interpolation
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            glitchSettings.intensity = Mathf.Lerp(startIntensity, tempSettings.intensity, smoothT);
            glitchSettings.timeScale = Mathf.Lerp(startTimeScale, tempSettings.timeScale, smoothT);
            glitchSettings.colorShift = Mathf.Lerp(startColorShift, tempSettings.colorShift, smoothT);
            glitchSettings.blockSize = Mathf.Lerp(startBlockSize, tempSettings.blockSize, smoothT);

            yield return null;
        }

        // Apply final preset
        glitchSettings.ApplyPreset(targetPreset);

        // Cleanup
        DestroyImmediate(tempSettings);
        activeTransitionCoroutine = null;
    }
}