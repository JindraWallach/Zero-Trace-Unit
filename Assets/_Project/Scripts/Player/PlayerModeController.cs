using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System;
using UnityEngine;

public enum PlayerMode { Normal, Hack }

/// <summary>
/// Controls player mode switching (Normal/Hack).
/// SRP: Single responsibility - mode management only.
/// Delegates glitch effect to GlitchController.
/// </summary>
public class PlayerModeController : MonoBehaviour, IInitializable
{
    public static PlayerModeController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private PlayerMode startMode = PlayerMode.Normal;

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
        // Apply initial glitch state
        ApplyGlitchForMode(CurrentMode);
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

        // Apply glitch effect
        ApplyGlitchForMode(mode);

        OnModeChanged?.Invoke(mode);
        Debug.Log($"[PlayerModeController] Mode: {mode}");
    }

    /// <summary>
    /// Apply glitch effect based on current mode.
    /// Normal Mode = Glitch disabled (SO stays with its values, just enabled = false)
    /// Hack Mode = Glitch enabled (SO values are used)
    /// </summary>
    private void ApplyGlitchForMode(PlayerMode mode)
    {
        if (GlitchController.Instance == null)
        {
            Debug.LogWarning("[PlayerModeController] GlitchController not found in scene!");
            return;
        }

        switch (mode)
        {
            case PlayerMode.Normal:
                // Just disable, keep SO values intact
                GlitchController.Instance.DisableGlitch();
                break;

            case PlayerMode.Hack:
                // Enable glitch (uses whatever is in SO)
                GlitchController.Instance.EnableGlitch();
                break;
        }
    }
}