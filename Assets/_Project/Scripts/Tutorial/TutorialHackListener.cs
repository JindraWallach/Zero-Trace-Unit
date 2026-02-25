// Scripts/Tutorial/TutorialHackListener.cs
using System;
using UnityEngine;

/// <summary>
/// SRP: Bridges HackManager puzzle result to the tutorial system.
/// 
/// HackManager uses local Action callbacks per-session, so this component
/// hooks into HackManager.RequestHack via a proxy pattern.
/// Raises a static event that TutorialMissionManager subscribes to.
/// 
/// Place this on the same GameObject as HackManager, or anywhere in the scene.
/// No Update(). Event-driven only.
/// </summary>
[DisallowMultipleComponent]
public class TutorialHackListener : MonoBehaviour
{
    // ── Static event ───────────────────────────────────────────────────────
    /// <summary>
    /// Raised when any hack puzzle succeeds during the tutorial.
    /// TutorialMissionManager subscribes to advance Phase 3.
    /// </summary>
    public static event Action OnPuzzleSucceeded;

    // ── Internal ───────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Subscribe to MissionManager state changes as secondary approach:
        // When the server is hacked, the mission state transitions which
        // confirms puzzle success without coupling directly to HackManager internals.
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnServerHackedEvent += OnServerHacked;
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnServerHackedEvent -= OnServerHacked;
    }

    private void OnServerHacked(UnityEngine.Vector3 _)
    {
        // Server hack = puzzle was solved successfully
        OnPuzzleSucceeded?.Invoke();
    }

    /// <summary>
    /// Can also be called directly by custom puzzle integration
    /// if you want explicit puzzle-complete signalling.
    /// </summary>
    public static void NotifyPuzzleSuccess()
    {
        OnPuzzleSucceeded?.Invoke();
    }
}