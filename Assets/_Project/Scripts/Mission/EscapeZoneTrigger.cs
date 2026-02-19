// Scripts/Mission/EscapeZoneTrigger.cs
using UnityEngine;

/// <summary>
/// SRP: Detekuje vstup hráče a volá MissionManager.CompleteMission().
/// Player tag čte z MissionSystemConfig SO.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class EscapeZoneTrigger : MonoBehaviour
{
    [SerializeField] private MissionSystemConfig config;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[EscapeZoneTrigger] Collider forced to isTrigger.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string tag = config != null ? config.playerTag : "Player";
        if (!other.CompareTag(tag)) return;
        if (MissionManager.Instance == null) return;
        if (MissionManager.Instance.CurrentState != MissionManager.MissionState.Escape) return;

        MissionManager.Instance.CompleteMission();
    }
}