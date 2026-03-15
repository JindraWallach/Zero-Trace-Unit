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
///   0 - Movement      → player walks into trigger zone (TutorialTriggerZone)
///   1 - Hack Mode     → player toggles hack mode once
///   2 - Hack Door     → player hacks the tutorial door (TutorialHackListener.OnDoorHackSucceeded)
///   3 - Hack Server   → player hacks the server      (TutorialHackListener.OnServerHackSucceeded)
///   4 - Stealth/Enemy → player reaches stealth trigger zone
///   5 - Escape        → EscapeZoneTrigger → MissionManager.CompleteMission (existing)
/// </summary>
[DisallowMultipleComponent]
public class TutorialMissionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static TutorialMissionManager Instance { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Fired whenever the tutorial advances to a new phase index.</summary>
    public static event Action<int> OnTutorialPhaseChanged;

    // ── State ──────────────────────────────────────────────────────────────
    public int CurrentPhase { get; private set; } = -1;

    // ── Phase constants ────────────────────────────────────────────────────
    public const int PHASE_MOVEMENT = 0;
    public const int PHASE_HACK_MODE = 1;
    public const int PHASE_HACK_DOOR = 2;
    public const int PHASE_HACK_SERVER = 3;
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
        TutorialHackListener.OnDoorHackSucceeded += OnDoorHacked;
        TutorialHackListener.OnServerHackSucceeded += OnServerHacked;
    }

    private void OnDisable()
    {
        TutorialHackListener.OnDoorHackSucceeded -= OnDoorHacked;
        TutorialHackListener.OnServerHackSucceeded -= OnServerHacked;

        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;
    }

    private void Start()
    {
        // PlayerModeController.Instance guaranteed by Start() (all Awake() ran)
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;
        else
            Debug.LogError("[TutorialMissionManager] PlayerModeController.Instance is null – Hack Mode phase will not advance.");

        // MissionManager activates objective[0] in its own Start().
        // Sync internal counter.
        CurrentPhase = PHASE_MOVEMENT;
        OnTutorialPhaseChanged?.Invoke(CurrentPhase);
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by TutorialTriggerZone (trigger colliders in the scene).
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

    // Phase 1: player switched to Hack mode
    private void OnModeChanged(PlayerMode mode)
    {
        if (CurrentPhase != PHASE_HACK_MODE) return;
        if (mode == PlayerMode.Hack)
            AdvanceInternal();
    }

    // Phase 2: tutorial door successfully hacked
    private void OnDoorHacked()
    {
        if (CurrentPhase != PHASE_HACK_DOOR) return;
        AdvanceInternal();
    }

    // Phase 3: mission server successfully hacked
    private void OnServerHacked()
    {
        if (CurrentPhase != PHASE_HACK_SERVER) return;
        AdvanceInternal();
    }
}