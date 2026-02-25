// Scripts/Tutorial/TutorialMissionManager.cs
using System;
using UnityEngine;

/// <summary>
/// SRP: Orchestrates the tutorial level (Level 0) objective sequence.
/// 
/// Listens to existing game system events – zero polling, no Update().
/// Advances objectives via MissionManager.AdvanceObjective().
/// 
/// Tutorial phases (in order):
///   0 - Movement        → player walks into trigger zone
///   1 - Hack Mode       → player toggles hack mode once
///   2 - Hack Server     → ServerCore fires MissionManager.OnServerHacked
///   3 - Puzzle          → HackManager puzzle succeeded
///   4 - Stealth/Enemy   → player reaches stealth trigger zone
///   5 - Escape          → EscapeZoneTrigger → MissionManager.CompleteMission (existing)
/// </summary>
[DisallowMultipleComponent]
public class TutorialMissionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static TutorialMissionManager Instance { get; private set; }

    // ── Events (other tutorial components listen here) ─────────────────────
    /// <summary>Fired when the tutorial advances to a new phase index.</summary>
    public static event Action<int> OnTutorialPhaseChanged;

    // ── State ──────────────────────────────────────────────────────────────
    public int CurrentPhase { get; private set; } = -1;

    // ── Phase constants ────────────────────────────────────────────────────
    public const int PHASE_MOVEMENT = 0;
    public const int PHASE_HACK_MODE = 1;
    public const int PHASE_HACK_SERVER = 2;
    public const int PHASE_PUZZLE = 3;
    public const int PHASE_STEALTH = 4;
    public const int PHASE_ESCAPE = 5;

    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        // Hack Mode phase: listen to PlayerModeController
        PlayerModeController.Instance.OnModeChanged += OnModeChanged;

        // Puzzle phase: listen to HackManager result
        TutorialHackListener.OnPuzzleSucceeded += OnPuzzleSucceeded;
    }

    private void OnDisable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;

        TutorialHackListener.OnPuzzleSucceeded -= OnPuzzleSucceeded;
    }

    private void Start()
    {
        // MissionManager auto-activates objective[0] on its own Start().
        // We just sync our internal phase counter.
        CurrentPhase = PHASE_MOVEMENT;
        OnTutorialPhaseChanged?.Invoke(CurrentPhase);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by TutorialTriggerZone or any external trigger component.
    /// Guards against out-of-order or duplicate advances.
    /// </summary>
    public void AdvanceFromPhase(int expectedPhase)
    {
        if (CurrentPhase != expectedPhase)
        {
            Debug.LogWarning($"[TutorialMissionManager] AdvanceFromPhase({expectedPhase}) ignored – current phase is {CurrentPhase}.");
            return;
        }

        AdvanceInternal();
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void AdvanceInternal()
    {
        CurrentPhase++;
        Debug.Log($"[TutorialMissionManager] Phase → {CurrentPhase}");

        MissionManager.Instance?.AdvanceObjective();
        OnTutorialPhaseChanged?.Invoke(CurrentPhase);
    }

    // Hack Mode phase handler
    private void OnModeChanged(PlayerMode mode)
    {
        if (CurrentPhase != PHASE_HACK_MODE) return;
        if (mode == PlayerMode.Hack)
            AdvanceInternal();
    }

    // Puzzle success handler (Phase 3)
    private void OnPuzzleSucceeded()
    {
        if (CurrentPhase != PHASE_PUZZLE) return;
        AdvanceInternal();
    }
}