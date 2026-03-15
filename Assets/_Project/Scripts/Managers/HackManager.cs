// Scripts/Managers/HackManager.cs
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
///
/// Raises OnAnyHackSucceeded(targetID) on every successful puzzle –
/// TutorialHackListener subscribes to this to drive tutorial phases
/// without any direct coupling to the tutorial system.
/// </summary>
public class HackManager : MonoBehaviour
{
    public static HackManager Instance { get; private set; }

    // ── Static event – zero coupling, safe in non-tutorial scenes ──────────
    /// <summary>
    /// Fired after every successful hack puzzle, with the target's TargetID.
    /// Only TutorialHackListener subscribes; in non-tutorial scenes nobody
    /// subscribes and the invocation is a no-op.
    /// </summary>
    public static event Action<string> OnAnyHackSucceeded;

    [Header("Puzzle System")]
    [SerializeField] private Transform puzzleSpawnParent;
    [SerializeField] private PuzzleFactory puzzleFactory;

    private readonly Dictionary<string, IHackTarget> registeredTargets = new();
    private PuzzleBase activePuzzle;
    private IHackTarget activeTarget;

    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Registration ───────────────────────────────────────────────────────

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

    public void CancelActivePuzzle()
    {
        activePuzzle?.CancelPuzzle();
    }

    // ── Hack Request ───────────────────────────────────────────────────────

    public bool RequestHack(IHackTarget target, Action onSuccess, Action onFail, Action onCancel = null)
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

        // Track which target is currently being hacked
        activeTarget = target;

        activePuzzle.OnSuccess += () => HandlePuzzleSuccess(onSuccess);
        activePuzzle.OnFail += () => HandlePuzzleFail(onFail);
        activePuzzle.OnCancel += () => HandlePuzzleCancel(onCancel);

        GameManager.Instance?.EnterPuzzleMode();
        UIManager.Instance?.EnterHackMode();
        activePuzzle.StartPuzzle();

        Debug.Log($"[HackManager] Hack started for {target.TargetID}");
        return true;
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void HandlePuzzleSuccess(Action callback)
    {
        Debug.Log("[HackManager] Puzzle SUCCESS");

        // Capture before CleanupPuzzle clears activeTarget
        string succeededID = activeTarget?.TargetID ?? string.Empty;

        CleanupPuzzle();

        GameManager.Instance?.ExitPuzzleMode();
        UIManager.Instance?.ExitHackMode();

        callback?.Invoke();

        // Notify tutorial system (no-op in non-tutorial scenes)
        if (!string.IsNullOrEmpty(succeededID))
            OnAnyHackSucceeded?.Invoke(succeededID);
    }

    private void HandlePuzzleFail(Action callback)
    {
        Debug.Log("[HackManager] Puzzle FAIL");
        CleanupPuzzle();
        GameManager.Instance?.ExitPuzzleMode();
        UIManager.Instance?.ExitHackMode();
        callback?.Invoke();
    }

    private void HandlePuzzleCancel(Action callback)
    {
        Debug.Log("[HackManager] Puzzle CANCELLED");
        CleanupPuzzle();
        GameManager.Instance?.ExitPuzzleMode();
        UIManager.Instance?.ExitHackMode();
        callback?.Invoke();
    }

    private void CleanupPuzzle()
    {
        if (activePuzzle != null)
        {
            Destroy(activePuzzle.gameObject);
            activePuzzle = null;
        }
        activeTarget = null;
    }
}