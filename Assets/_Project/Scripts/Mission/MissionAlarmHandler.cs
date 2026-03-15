// Scripts/Mission/MissionAlarmHandler.cs
using UnityEngine;

/// <summary>
/// SRP: Reaguje na MissionManager eventy – spouští alarm a aktivuje escape zónu.
///
/// Implementuje IInitializable – DI zavolá Initialize(di) automaticky.
/// Stejný pattern jako GameManager, FlashlightController, atd.
/// IAlarmSystem je tažen z di.AlarmSystem – žádný [SerializeField] na konkrétní třídu.
///
/// EscapeZone je [SerializeField] – jde o scene referenci, ne o service.
/// Event-driven: no Update(), no polling.
/// </summary>
[DisallowMultipleComponent]
public class MissionAlarmHandler : MonoBehaviour, IInitializable
{
    [Header("Scene References")]
    [Tooltip("EscapeZone GO – deaktivovaný na začátku hry")]
    [SerializeField] private GameObject escapeZone;

    // Injektováno přes DI – není [SerializeField]
    private SecurityAlarmSystem _alarmSystem;

    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Voláno DependencyInjectorem v Start().
    /// Přidej MissionAlarmHandler GO do DI.objectsToInitialize v Inspectoru.
    /// </summary>
    public void Initialize(DependencyInjector di)
    {
        _alarmSystem = di.AlarmSystem;

        if (_alarmSystem == null)
            Debug.LogError("[MissionAlarmHandler] IAlarmSystem not found in DependencyInjector!", this);
    }

    private void Awake()
    {
        if (escapeZone != null)
            escapeZone.SetActive(false);
        else
            Debug.LogWarning("[MissionAlarmHandler] EscapeZone not assigned!", this);
    }

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
            Subscribe(MissionManager.Instance);
        else
            StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
            Unsubscribe(MissionManager.Instance);
    }

    // ── Event handlers ─────────────────────────────────────────────────────

    private void HandleServerHacked(UnityEngine.Vector3 serverPosition)
    {
        if (_alarmSystem != null)
            _alarmSystem.TriggerAlarm(serverPosition);
        else
            Debug.LogWarning("[MissionAlarmHandler] IAlarmSystem is null – byl zavolán Initialize()?");
    }

    private void HandleEscapeActivated()
    {
        if (escapeZone != null)
            escapeZone.SetActive(true);
        else
            Debug.LogWarning("[MissionAlarmHandler] EscapeZone not assigned!");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void Subscribe(MissionManager mm)
    {
        mm.OnServerHackedEvent += HandleServerHacked;
        mm.OnEscapeActivated += HandleEscapeActivated;
    }

    private void Unsubscribe(MissionManager mm)
    {
        mm.OnServerHackedEvent -= HandleServerHacked;
        mm.OnEscapeActivated -= HandleEscapeActivated;
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        yield return null;
        if (MissionManager.Instance != null)
            Subscribe(MissionManager.Instance);
        else
            Debug.LogError("[MissionAlarmHandler] MissionManager not found after one frame!");
    }
}