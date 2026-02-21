// Scripts/Managers/MissionManager.cs
using System;
using UnityEngine;

/// <summary>
/// SRP: Owns ONLY mission state. Raises events – other systems react.
/// No references to UI, alarm, or escape zone here.
///
/// Extended with MissionDataSO objective progression.
/// All existing API preserved – EscapeZoneTrigger, MissionAlarmHandler,
/// MissionUIHandler and ServerCore require ZERO changes.
///
/// States:  Infiltration → DataDownloaded → Escape → Completed
///
/// Objective flow:
///   Start            → objectives[0] active  ("FIND AND DOWNLOAD DATA")
///   OnServerHacked() → objectives[1] active  ("ESCAPE")
///                    → state = Escape
///   CompleteMission()→ state = Completed
/// </summary>
public class MissionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static MissionManager Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Mission Data")]
    [Tooltip("Assign MissionData SO for this level (holds ordered objectives list)")]
    [SerializeField] private MissionDataSO missionData;

    // ── State enum (EscapeZoneTrigger uses MissionManager.MissionState.Escape) ──
    public enum MissionState { Infiltration, DataDownloaded, Escape, Completed }

    public MissionState CurrentState { get; private set; } = MissionState.Infiltration;

    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when a new objective activates.
    /// MissionUI listens to show HUD popup and update pause menu text.
    /// </summary>
    public static event Action<MissionObjectiveSO> OnObjectiveChanged;

    /// <summary>Payload: server world position. MissionAlarmHandler listens.</summary>
    public event Action<Vector3> OnServerHackedEvent;

    /// <summary>MissionAlarmHandler listens to activate escape zone GO.</summary>
    public event Action OnEscapeActivated;

    /// <summary>MissionUIHandler listens to show completion overlay.</summary>
    public event Action OnMissionCompleted;

    // ── Runtime ────────────────────────────────────────────────────────────
    private int _currentObjectiveIndex = -1;

    /// <summary>Currently active objective, null if none or missionData not assigned.</summary>
    public MissionObjectiveSO CurrentObjective =>
        (missionData != null && _currentObjectiveIndex >= 0 &&
         _currentObjectiveIndex < missionData.objectives.Count)
            ? missionData.objectives[_currentObjectiveIndex]
            : null;

    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (missionData == null)
            Debug.LogWarning("[MissionManager] MissionData SO not assigned – objective system disabled.", this);
    }

    private void Start()
    {
        // Activate first objective immediately on level start
        ActivateObjectiveAt(0);
    }

    // ── Public API (unchanged – called by existing scripts) ────────────────

    /// <summary>
    /// Called by ServerCore after successful hack.
    /// Advances to next objective and transitions state to Escape.
    /// </summary>
    public void OnServerHacked(Vector3 serverPosition)
    {
        if (CurrentState != MissionState.Infiltration)
        {
            Debug.LogWarning("[MissionManager] OnServerHacked ignored – wrong state.");
            return;
        }

        CurrentState = MissionState.DataDownloaded;
        Debug.Log("[MissionManager] State → DataDownloaded");

        OnServerHackedEvent?.Invoke(serverPosition);

        // Advance objective (index 0 → index 1, i.e. "ESCAPE")
        ActivateObjectiveAt(_currentObjectiveIndex + 1);

        CurrentState = MissionState.Escape;
        Debug.Log("[MissionManager] State → Escape");

        OnEscapeActivated?.Invoke();
    }

    /// <summary>
    /// Called by EscapeZoneTrigger when player reaches extraction.
    /// </summary>
    public void CompleteMission()
    {
        if (CurrentState != MissionState.Escape)
        {
            Debug.LogWarning("[MissionManager] CompleteMission ignored – wrong state.");
            return;
        }

        CurrentState = MissionState.Completed;
        Debug.Log("[MissionManager] State → Completed");

        OnMissionCompleted?.Invoke();
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void ActivateObjectiveAt(int index)
    {
        if (missionData == null) return;
        if (index < 0 || index >= missionData.objectives.Count) return;

        var objective = missionData.objectives[index];
        if (objective == null)
        {
            Debug.LogError($"[MissionManager] Objective at index {index} is null!", this);
            return;
        }

        _currentObjectiveIndex = index;
        Debug.Log($"[MissionManager] Objective [{index}]: '{objective.objectiveText}'");

        OnObjectiveChanged?.Invoke(objective);
    }

    private void OnDestroy()
    {
        // Safety: restore timescale if scene is unloaded while frozen
        Time.timeScale = 1f;
    }
}