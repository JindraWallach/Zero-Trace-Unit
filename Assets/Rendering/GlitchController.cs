using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    private ScreenGlitchFeature glitchFeature;
    private Coroutine activeGlitchCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        FindGlitchFeature();
        SubscribeToEvents();

        if (glitchSettings != null)
        {
            glitchSettings.ApplyPreset(GlitchEffectSettings.GlitchPreset.Minimal);
            glitchSettings.enabled = false;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (Instance == this) Instance = null;
    }

    private void FindGlitchFeature()
    {
        if (rendererData == null) { Debug.LogWarning("[GlitchController] RendererData not assigned!"); return; }

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is ScreenGlitchFeature screenGlitch)
            {
                glitchFeature = screenGlitch;
                if (glitchSettings == null) glitchSettings = screenGlitch.SettingsAsset;
                return;
            }
        }

        Debug.LogWarning("[GlitchController] ScreenGlitchFeature not found!");
    }

    private void SubscribeToEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied += TriggerDeathGlitch;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
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

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.InPuzzle)
            TriggerPulseGlitch();
    }

    // === PUBLIC API ===

    public void EnableGlitch()
    {
        if (glitchSettings == null) return;
        glitchSettings.enabled = true;
    }

    public void DisableGlitch()
    {
        if (glitchSettings == null) return;
        glitchSettings.enabled = false;
    }

    public bool IsGlitchEnabled() => glitchSettings != null && glitchSettings.enabled;

    // === DEATH ===

    public void TriggerDeathGlitch()
    {
        if (glitchSettings == null) return;

        if (activeGlitchCoroutine != null) StopCoroutine(activeGlitchCoroutine);
        activeGlitchCoroutine = StartCoroutine(DeathGlitchCoroutine());
    }

    private IEnumerator DeathGlitchCoroutine()
    {
        glitchSettings.ApplyPreset(deathPreset);
        glitchSettings.enabled = true;

        float elapsed = 0f;
        while (elapsed < deathGlitchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = deathGlitchCurve.Evaluate(elapsed / deathGlitchDuration);
            glitchSettings.intensity = Mathf.Lerp(0.003f, 0.15f, t);
            yield return null;
        }

        glitchSettings.intensity = 0.15f;
        activeGlitchCoroutine = null;
    }

    // === PULSE ===

    public void TriggerPulseGlitch()
    {
        if (glitchSettings == null || !glitchSettings.enabled) return;
        StartCoroutine(PulseGlitchCoroutine());
    }

    private IEnumerator PulseGlitchCoroutine()
    {
        float startIntensity = glitchSettings.intensity;
        float targetIntensity = Mathf.Min(startIntensity * pulseIntensityMultiplier, 0.15f);
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin((elapsed / pulseDuration) * Mathf.PI);
            glitchSettings.intensity = Mathf.Lerp(startIntensity, targetIntensity, pulse);
            yield return null;
        }

        glitchSettings.intensity = startIntensity;
    }
}