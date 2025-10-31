using Synty.AnimationBaseLocomotion.Samples;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central hack orchestration system (singleton).
/// - Registers all IHackTarget objects
/// - Spawns puzzles via PuzzleFactory
/// - Handles success/fail callbacks
/// </summary>
public class HackManager : MonoBehaviour
{
    public static HackManager Instance { get; private set; }

    [Header("Puzzle System")]
    [SerializeField] private Transform puzzleSpawnParent;
    [SerializeField] private PuzzleFactory puzzleFactory;

    private InputReader inputReader;
    private SampleCameraController cameraController;
    private readonly Dictionary<string, IHackTarget> registeredTargets = new();
    private PuzzleBase activePuzzle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var di = FindFirstObjectByType<DependencyInjector>();
        if (di != null)
            inputReader = di.InputReader;
    }

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.onEscapePressed += OnEscapePressed;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.onEscapePressed -= OnEscapePressed;
    }

    private void OnEscapePressed()
    {
        if (activePuzzle != null)
        {
            activePuzzle.CancelPuzzle();
        }
    }

    // === Registration ===
    public void RegisterTarget(IHackTarget target)
    {
        if (target == null || string.IsNullOrEmpty(target.TargetID)) return;
        if (!registeredTargets.ContainsKey(target.TargetID))
        {
            registeredTargets[target.TargetID] = target;
            Debug.Log($"[HackManager] Registered target: {target.TargetID}");
        }
    }

    public void UnregisterTarget(IHackTarget target)
    {
        if (target != null && !string.IsNullOrEmpty(target.TargetID))
            registeredTargets.Remove(target.TargetID);
    }

    // === Hack Request ===
    public bool RequestHack(IHackTarget target, Action onSuccess, Action onFail)
    {
        if (activePuzzle != null)
        {
            Debug.LogWarning("[HackManager] Hack already in progress.");
            return false;
        }

        if (!target.IsHackable)
        {
            Debug.LogWarning($"[HackManager] Target {target.TargetID} is not hackable.");
            return false;
        }

        // Spawn puzzle
        var puzzlePrefab = puzzleFactory.GetPuzzlePrefab(target);
        if (puzzlePrefab == null)
        {
            Debug.LogError("[HackManager] No puzzle prefab available.");
            return false;
        }

        var instance = Instantiate(puzzlePrefab, puzzleSpawnParent);
        activePuzzle = instance.GetComponent<PuzzleBase>();

        if (activePuzzle == null)
        {
            Debug.LogError("[HackManager] Puzzle prefab missing PuzzleBase component.");
            Destroy(instance);
            return false;
        }

        // Setup callbacks
        activePuzzle.OnSuccess += () => HandlePuzzleSuccess(onSuccess);
        activePuzzle.OnFail += () => HandlePuzzleFail(onFail);
        activePuzzle.OnCancel += () => HandlePuzzleCancel(onFail);

        // Enter hack mode
        UIManager.Instance?.EnterHackMode();
        activePuzzle.StartPuzzle();

        Debug.Log($"[HackManager] Hack started for {target.TargetID}");
        return true;
    }

    private void HandlePuzzleSuccess(Action callback)
    {
        Debug.Log("[HackManager] Puzzle SUCCESS");
        CleanupPuzzle();
        UIManager.Instance?.ExitHackMode();
        RestorePlayerInput(); 
        callback?.Invoke();
    }

    private void HandlePuzzleFail(Action callback)
    {
        Debug.Log("[HackManager] Puzzle FAIL");
        CleanupPuzzle();
        UIManager.Instance?.ExitHackMode();
        RestorePlayerInput(); 
        callback?.Invoke();
    }

    private void HandlePuzzleCancel(Action callback)
    {
        Debug.Log("[HackManager] Puzzle CANCELLED");
        CleanupPuzzle();
        UIManager.Instance?.ExitHackMode();
        RestorePlayerInput(); 
        callback?.Invoke();
    }

    private void CleanupPuzzle()
    {
        if (activePuzzle != null)
        {
            Destroy(activePuzzle.gameObject);
            activePuzzle = null;
        }
    }

    public void BlockPlayerInput(InputReader reader, SampleCameraController cam)
    {
        inputReader = reader;
        cameraController = cam;

        if (inputReader != null)
            inputReader.DisableInputs(new[] { "Exit" }); // Enable only Exit

        if (cameraController != null)
            cameraController.enabled = false;
    }

    private void RestorePlayerInput()
    {
        if (inputReader != null)
            inputReader.EnableAllInputs();

        if (cameraController != null)
            cameraController.enabled = true;

        inputReader = null;
        cameraController = null;
    }
}