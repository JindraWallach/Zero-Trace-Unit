using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System;
using UnityEngine;

public enum PlayerMode { Normal, Hack }

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

    public void Initialize(DependencyInjector di)
    {
        inputReader = di.InputReader;
        inputReader.onHackModeToggle += ToggleMode;
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.onHackModeToggle -= ToggleMode;
    }

    private void ToggleMode()
    {
        var newMode = CurrentMode == PlayerMode.Normal ? PlayerMode.Hack : PlayerMode.Normal;
        SetMode(newMode);
    }

    private void SetMode(PlayerMode mode)
    {
        if (CurrentMode == mode) return;

        CurrentMode = mode;

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

        OnModeChanged?.Invoke(mode);
        Debug.Log($"[PlayerModeController] Mode: {mode}");
    }
}