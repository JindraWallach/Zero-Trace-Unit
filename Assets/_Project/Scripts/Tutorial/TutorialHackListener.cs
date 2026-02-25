// Scripts/Tutorial/TutorialHackListener.cs
using System;
using UnityEngine;

/// <summary>
/// SRP: Bridges HackManager hack results to the tutorial system.
///
/// Listens to HackManager.OnAnyHackSucceeded (static event, no null-ref risk)
/// and re-raises two targeted static events that TutorialMissionManager
/// subscribes to – one for the tutorial door hack, one for the server hack.
///
/// ┌─────────────────────────────────────────────────────────────────┐
/// │ Zero coupling to door/server scripts.                           │
/// │ Safe in non-tutorial scenes – nobody subscribes → no-op.       │
/// │ No Update(). Event-driven only.                                 │
/// └─────────────────────────────────────────────────────────────────┘
///
/// Setup:
///   - Place anywhere in the tutorial scene.
///   - Assign tutorialDoorTargetID to match the HackableDoor's TargetID.
///   - Assign serverTargetID to match the ServerCore's TargetID
///     (default "ServerCore", set in ServerCoreConfig SO).
/// </summary>
[DisallowMultipleComponent]
public class TutorialHackListener : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Target IDs (must match IHackTarget.TargetID on the objects)")]
    [Tooltip("TargetID of the tutorial door the player must hack (Phase 2)")]
    [SerializeField] private string tutorialDoorTargetID = "Door_Tutorial";

    [Tooltip("TargetID of the mission server the player must hack (Phase 3 → server hack)")]
    [SerializeField] private string serverTargetID = "ServerCore";

    // ── Static events ──────────────────────────────────────────────────────
    /// <summary>Phase 2: tutorial door was successfully hacked.</summary>
    public static event Action OnDoorHackSucceeded;

    /// <summary>Phase 3: mission server was successfully hacked.</summary>
    public static event Action OnServerHackSucceeded;

    // ──────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        HackManager.OnAnyHackSucceeded += HandleAnyHackSucceeded;
    }

    private void OnDisable()
    {
        HackManager.OnAnyHackSucceeded -= HandleAnyHackSucceeded;
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void HandleAnyHackSucceeded(string targetID)
    {
        if (targetID == tutorialDoorTargetID)
        {
            Debug.Log("[TutorialHackListener] Door hack succeeded → OnDoorHackSucceeded");
            OnDoorHackSucceeded?.Invoke();
        }
        else if (targetID == serverTargetID)
        {
            Debug.Log("[TutorialHackListener] Server hack succeeded → OnServerHackSucceeded");
            OnServerHackSucceeded?.Invoke();
        }
    }
}