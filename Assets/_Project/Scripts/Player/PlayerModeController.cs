using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System;
using UnityEngine;

public enum PlayerMode { Normal, Hack }

/// <summary>
/// Controls player mode switching (Normal/Hack).
/// Automatically applies glitch effect when entering Hack mode.
/// </summary>
public class PlayerModeController : MonoBehaviour, IInitializable
{
    public static PlayerModeController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private PlayerMode startMode = PlayerMode.Normal;
    [SerializeField] private GlitchController glitchController;
    [Header("Glitch Integration")]
    [SerializeField] private GlitchEffectSettings hackModeGlitchSettings;

    [SerializeField] private float glitchTransitionDuration = 0.5f;
    [Tooltip("Apply None preset when in Normal mode")]
    [SerializeField] private bool disableGlitchInNormalMode = true;

    public event Action<PlayerMode> OnModeChanged;
    public PlayerMode CurrentMode { get; private set; }

    private InputReader inputReader;
    private ToolController toolController;
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        toolController = GetComponent<ToolController>();
        CurrentMode = startMode;
    }

    private void Start()
    {
        // Find GlitchController in scene
        glitchController = GetComponent<GlitchController>();

        // Apply initial glitch state based on start mode
        ApplyModeGlitchEffect(CurrentMode, immediate: true);
    }

    public void Initialize(DependencyInjector di)
    {
        inputReader = di.InputReader;
        inputReader.onHackModeToggle += ToggleMode;
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.onHackModeToggle -= ToggleMode;

        if (Instance == this)
            Instance = null;
    }

    private void ToggleMode()
    {
        var newMode = CurrentMode == PlayerMode.Normal ? PlayerMode.Hack : PlayerMode.Normal;
        SetMode(newMode);
    }

    public void SetMode(PlayerMode mode)
    {
        if (CurrentMode == mode) return;

        CurrentMode = mode;

        // Tool management
        if (mode == PlayerMode.Hack)
        {
            toolController?.ShowTool();
            toolController?.StartScan();
        }
        else
        {
            toolController?.HideTool();
            toolController?.StopScan();
        }

        // Apply glitch effect for mode
        ApplyModeGlitchEffect(mode, immediate: false);

        OnModeChanged?.Invoke(mode);
        Debug.Log($"[PlayerModeController] Mode: {mode}");
    }

    /// <summary>
    /// Apply glitch effect based on current mode.
    /// </summary>
    private void ApplyModeGlitchEffect(PlayerMode mode, bool immediate)
    {
        if (glitchController == null || hackModeGlitchSettings == null)
            return;

        switch (mode)
        {
            case PlayerMode.Hack:
                // Apply hack mode glitch settings
                if (immediate)
                {
                    CopySettingsToGlitchController(hackModeGlitchSettings);
                }
                else
                {
                    // Smooth transition to hack mode glitch
                    StartCoroutine(TransitionToHackGlitch());
                }
                break;

            case PlayerMode.Normal:
                // Disable or reset glitch
                if (disableGlitchInNormalMode)
                {
                    if (immediate)
                    {
                        glitchController.SetEnabled(false);
                    }
                    else
                    {
                        glitchController.TransitionToPreset(
                            GlitchEffectSettings.GlitchPreset.None,
                            glitchTransitionDuration
                        );
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Smooth transition to hack mode glitch effect.
    /// </summary>
    private System.Collections.IEnumerator TransitionToHackGlitch()
    {
        if (glitchController == null || hackModeGlitchSettings == null)
            yield break;

        // Get current settings reference
        var currentSettings = glitchController.GetComponent<ScreenGlitchFeature>()?.SettingsAsset;
        if (currentSettings == null)
        {
            // Fallback: instant apply
            CopySettingsToGlitchController(hackModeGlitchSettings);
            yield break;
        }

        // Store start values
        float startIntensity = currentSettings.intensity;
        float startTimeScale = currentSettings.timeScale;
        float startColorShift = currentSettings.colorShift;
        float startBlockSize = currentSettings.blockSize;

        float elapsed = 0f;

        // Enable if disabled
        currentSettings.enabled = true;

        while (elapsed < glitchTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / glitchTransitionDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Interpolate values
            currentSettings.intensity = Mathf.Lerp(startIntensity, hackModeGlitchSettings.intensity, smoothT);
            currentSettings.timeScale = Mathf.Lerp(startTimeScale, hackModeGlitchSettings.timeScale, smoothT);
            currentSettings.colorShift = Mathf.Lerp(startColorShift, hackModeGlitchSettings.colorShift, smoothT);
            currentSettings.blockSize = Mathf.Lerp(startBlockSize, hackModeGlitchSettings.blockSize, smoothT);

            yield return null;
        }

        // Final copy of all settings
        CopySettingsToGlitchController(hackModeGlitchSettings);
    }

    /// <summary>
    /// Copy settings from source SO to glitch controller's settings.
    /// </summary>
    private void CopySettingsToGlitchController(GlitchEffectSettings source)
    {
        if (glitchController == null || source == null)
            return;

        var feature = FindFirstObjectByType<ScreenGlitchFeature>();
        if (feature == null)
        {
            Debug.LogWarning("[PlayerModeController] ScreenGlitchFeature not found!");
            return;
        }

        var targetSettings = feature.SettingsAsset;
        if (targetSettings == null)
        {
            Debug.LogWarning("[PlayerModeController] Target glitch settings not assigned!");
            return;
        }

        // Copy all values
        targetSettings.intensity = source.intensity;
        targetSettings.timeScale = source.timeScale;
        targetSettings.colorShift = source.colorShift;
        targetSettings.blockSize = source.blockSize;
        targetSettings.scanlineIntensity = source.scanlineIntensity;
        targetSettings.inversionIntensity = source.inversionIntensity;
        targetSettings.verticalShift = source.verticalShift;
        targetSettings.noiseFrequency = source.noiseFrequency;
        targetSettings.enabled = source.enabled;
        targetSettings.updateFrequency = source.updateFrequency;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(targetSettings);
#endif
    }

    /// <summary>
    /// Manually trigger glitch pulse (e.g., on successful hack).
    /// </summary>
    public void TriggerGlitchPulse()
    {
        if (glitchController != null)
            glitchController.TriggerPulseGlitch();
    }

    // === EDITOR TESTING ===

    [ContextMenu("Test Switch to Hack Mode")]
    private void TestHackMode()
    {
        if (!Application.isPlaying) return;
        SetMode(PlayerMode.Hack);
    }

    [ContextMenu("Test Switch to Normal Mode")]
    private void TestNormalMode()
    {
        if (!Application.isPlaying) return;
        SetMode(PlayerMode.Normal);
    }

    [ContextMenu("Test Glitch Pulse")]
    private void TestGlitchPulse()
    {
        if (!Application.isPlaying) return;
        TriggerGlitchPulse();
    }
}