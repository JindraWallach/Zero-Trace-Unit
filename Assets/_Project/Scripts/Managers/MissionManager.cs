// Scripts/Mission/MissionManager.cs
using System;
using UnityEngine;

/// <summary>
/// SRP: Owns ONLY mission state. Raises events – other systems react.
/// No references to UI, alarm, or escape zone here.
///
/// States: Infiltration → DataDownloaded → Escape → Completed
/// </summary>
public class MissionManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static MissionManager Instance { get; private set; }

    // ── State ──────────────────────────────────────────────────────────────
    public enum MissionState { Infiltration, DataDownloaded, Escape, Completed }

    public MissionState CurrentState { get; private set; } = MissionState.Infiltration;

    // ── Events (loose coupling – listeners register themselves) ────────────
    public event Action<Vector3> OnServerHackedEvent;   // payload: server world position
    public event Action OnEscapeActivated;
    public event Action OnMissionCompleted;

    // ───────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API (called by other systems) ───────────────────────────────

    /// <summary>Called by ServerObjective after successful hack.</summary>
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

        CurrentState = MissionState.Escape;
        Debug.Log("[MissionManager] State → Escape");

        OnEscapeActivated?.Invoke();
    }

    /// <summary>Called by EscapeZoneTrigger when player reaches extraction.</summary>
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

    private void OnDestroy()
    {
        // Safety: restore timescale if scene is unloaded while frozen
        Time.timeScale = 1f;
    }
}