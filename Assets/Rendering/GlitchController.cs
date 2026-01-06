using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Runtime controller for screen glitch effect.
/// Dynamically adjusts glitch intensity based on game events.
/// Example: Trigger glitch on player death, enemy detection, etc.
/// </summary>
public class GlitchController : MonoBehaviour
{
    [Header("Renderer Setup")]
    [SerializeField] private UniversalRendererData rendererData;

    [Header("Death Glitch Config")]
    [SerializeField] private float deathGlitchDuration = 2f;
    [SerializeField] private AnimationCurve deathGlitchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float maxDeathIntensity = 0.08f;

    [Header("Pulse Glitch Config")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float pulseIntensity = 0.05f;

    private ScreenGlitchFeature glitchFeature;
    private ScreenGlitchFeature.Settings glitchSettings;
    private Coroutine activeGlitchCoroutine;
    private float baseIntensity;

    private void Start()
    {
        FindGlitchFeature();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // === SETUP ===

    private void FindGlitchFeature()
    {
        if (rendererData == null)
        {
            // Try to find renderer data automatically
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                // Note: This requires reflection or manual assignment in inspector
                Debug.LogWarning("[GlitchController] Please assign UniversalRendererData in inspector!");
            }
            return;
        }

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is ScreenGlitchFeature screenGlitch)
            {
                glitchFeature = screenGlitch;
                glitchSettings = screenGlitch.GetSettings();
                baseIntensity = glitchSettings.intensity;
                Debug.Log("[GlitchController] Found ScreenGlitchFeature");
                return;
            }
        }

        Debug.LogWarning("[GlitchController] ScreenGlitchFeature not found in renderer!");
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
        }
    }

    // === PUBLIC API ===

    /// <summary>
    /// Trigger death glitch sequence.
    /// Called by GameManager on player death.
    /// </summary>
    public void TriggerDeathGlitch()
    {
        if (glitchSettings == null)
        {
            Debug.LogWarning("[GlitchController] Glitch settings not initialized!");
            return;
        }

        if (activeGlitchCoroutine != null)
            StopCoroutine(activeGlitchCoroutine);

        activeGlitchCoroutine = StartCoroutine(DeathGlitchCoroutine());
    }

    /// <summary>
    /// Trigger short pulse glitch.
    /// Useful for feedback on hacking, puzzle start, etc.
    /// </summary>
    public void TriggerPulseGlitch()
    {
        if (glitchSettings == null)
        {
            Debug.LogWarning("[GlitchController] Glitch settings not initialized!");
            return;
        }

        StartCoroutine(PulseGlitchCoroutine());
    }

    /// <summary>
    /// Set glitch intensity directly (modifies Settings).
    /// </summary>
    public void SetGlitchIntensity(float intensity)
    {
        if (glitchSettings == null) return;

        glitchSettings.intensity = Mathf.Clamp(intensity, 0f, 0.1f);

        // Force renderer to update (mark dirty)
        if (rendererData != null)
        {
            rendererData.SetDirty();
        }
    }

    /// <summary>
    /// Reset glitch to base intensity.
    /// </summary>
    public void ResetGlitchIntensity()
    {
        SetGlitchIntensity(baseIntensity);
    }

    // === COROUTINES ===

    private IEnumerator DeathGlitchCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < deathGlitchDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for death sequence
            float t = elapsed / deathGlitchDuration;
            float intensity = deathGlitchCurve.Evaluate(t) * maxDeathIntensity;

            SetGlitchIntensity(intensity);

            yield return null;
        }

        ResetGlitchIntensity();
        activeGlitchCoroutine = null;
    }

    private IEnumerator PulseGlitchCoroutine()
    {
        float elapsed = 0f;
        float startIntensity = glitchSettings.intensity;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;

            // Pulse: 0 -> max -> 0
            float pulse = Mathf.Sin(t * Mathf.PI) * pulseIntensity;
            SetGlitchIntensity(startIntensity + pulse);

            yield return null;
        }

        SetGlitchIntensity(startIntensity);
    }

    // === EDITOR TESTING ===

    [ContextMenu("Test Death Glitch")]
    private void TestDeathGlitch()
    {
        TriggerDeathGlitch();
    }

    [ContextMenu("Test Pulse Glitch")]
    private void TestPulseGlitch()
    {
        TriggerPulseGlitch();
    }

    [ContextMenu("Reset Glitch")]
    private void TestResetGlitch()
    {
        ResetGlitchIntensity();
    }
}