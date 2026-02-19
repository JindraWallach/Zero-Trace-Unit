// Scripts/Mission/EscapeZoneTrigger.cs
using UnityEngine;

/// <summary>
/// SRP: Detects player entry and calls MissionManager.CompleteMission().
/// Registers/unregisters itself via MissionManager events – no polling, no Update().
///
/// Setup:
///   1. Create GameObject "EscapeZone" with a Trigger Collider.
///   2. Attach this script.
///   3. Disable the GameObject in the scene – MissionManager.OnEscapeActivated enables it.
///   4. Assign the reference in MissionAlarmHandler (or MissionManager) Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class EscapeZoneTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    // ───────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Enforce trigger
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[EscapeZoneTrigger] Collider forced to isTrigger.");
        }
    }

    private void OnEnable()
    {
        Debug.Log("[EscapeZoneTrigger] Escape zone active.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentState != MissionManager.MissionState.Escape) return;

        MissionManager.Instance.CompleteMission();
    }
}