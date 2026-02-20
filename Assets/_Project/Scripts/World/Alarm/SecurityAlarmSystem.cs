using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Security alarm system triggered by failed hacking attempts.
/// Handles visual effects and enemy alerting.
/// SRP: Only manages alarm state and effects.
/// Performance: Coroutine-based, no Update().
///
/// Rozšíření pro ServerCore:
///   TriggerAlarm()          – stackuje čas (max stackCap), pokud alarm již běží
///   TriggerPermanentAlarm() – alarm bez konce, nelze zastavit (ServerCore hack success)
/// </summary>
public class SecurityAlarmSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SecurityAlarmConfig config;

    [Header("Scene References")]
    [Tooltip("Lights to flash during alarm (optional, auto-finds if empty)")]
    [SerializeField] private Light[] roomLights;

    [Header("Debug")]
    [SerializeField] private bool isAlarmActive;
    [SerializeField] private float timeRemaining;

    // Events
    public event Action OnAlarmTriggered;
    public event Action OnAlarmEnded;

    // State
    private bool alarmActive;
    private bool alarmPermanent;          // true = nekonečný alarm (ServerCore hack success)
    private float alarmTimeRemaining;     // aktuální zbývající čas (pro stack logiku)
    private Coroutine alarmCoroutine;
    private Coroutine lightFlashCoroutine;
    private AudioSource audioSource;

    [Header("Stack Settings")]
    [Tooltip("Kolik sekund přidá každý další fail hack k běžícímu alarmu")]
    [SerializeField] private float stackDuration = 30f;
    [Tooltip("Maximální celkový čas alarmu při stackování (seconds)")]
    [SerializeField] private float stackCap = 60f;

    // Cache
    private Dictionary<Light, Color> originalLightColors = new Dictionary<Light, Color>();
    private Dictionary<Light, float> originalLightIntensities = new Dictionary<Light, float>();

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("[SecurityAlarmSystem] Missing SecurityAlarmConfig!", this);
            enabled = false;
            return;
        }

        // Setup audio source
        if (config.alarmSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = config.alarmSound;
            audioSource.volume = config.alarmVolume;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }

        // Auto-find lights if not assigned
        if (roomLights == null || roomLights.Length == 0)
        {
            roomLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            if (config.debugLog)
                Debug.Log($"[SecurityAlarmSystem] Auto-found {roomLights.Length} lights", this);
        }

        // Cache original light settings
        CacheLightSettings();
    }

    /// <summary>
    /// Trigger alarm at specific position.
    /// Pokud alarm již běží: přidá stackDuration (max stackCap).
    /// Pokud alarm neběží: spustí nový alarm se stackDuration.
    /// Permanentní alarm (TriggerPermanentAlarm) nelze přepsat.
    /// </summary>
    public void TriggerAlarm(Vector3 alarmPosition)
    {
        // Permanentní alarm – ignoruj další volání
        if (alarmPermanent)
        {
            if (config.debugLog)
                Debug.Log("[SecurityAlarmSystem] Permanent alarm active – stack ignored.", this);
            return;
        }

        if (alarmActive)
        {
            // Stack: přidej čas, max stackCap
            float added = Mathf.Min(stackDuration, stackCap - alarmTimeRemaining);
            if (added > 0f)
            {
                alarmTimeRemaining += added;
                if (config.debugLog)
                    Debug.Log($"[SecurityAlarmSystem] Alarm stacked +{added:F0}s → {alarmTimeRemaining:F0}s remaining (cap {stackCap}s)", this);
            }
            else if (config.debugLog)
            {
                Debug.Log($"[SecurityAlarmSystem] Alarm already at stack cap ({stackCap}s).", this);
            }
            return;
        }

        if (config.debugLog)
            Debug.Log($"[SecurityAlarmSystem] ALARM TRIGGERED at {alarmPosition} for {stackDuration}s", this);

        alarmActive = true;
        alarmTimeRemaining = stackDuration;
        isAlarmActive = true;
        OnAlarmTriggered?.Invoke();

        alarmCoroutine = StartCoroutine(AlarmSequence(alarmPosition));

        if (config.enableFlashingLights)
            lightFlashCoroutine = StartCoroutine(FlashLightsCoroutine());

        if (audioSource != null)
            audioSource.Play();
    }

    /// <summary>
    /// Spustí permanentní alarm bez časového limitu – nelze zastavit.
    /// Voláno při úspěšném hacknutí ServerCore.
    /// </summary>
    public void TriggerPermanentAlarm(Vector3 alarmPosition)
    {
        alarmPermanent = true;

        if (config.debugLog)
            Debug.Log("[SecurityAlarmSystem] PERMANENT ALARM triggered (ServerCore hacked).", this);

        if (alarmActive)
        {
            // Alarm již běží – jen nastav permanent flag, efekty pokračují
            // Zastav původní coroutine která by alarm ukončila
            if (alarmCoroutine != null)
            {
                StopCoroutine(alarmCoroutine);
                alarmCoroutine = null;
            }
            // Nastav timeRemaining na infinity pro debug
            timeRemaining = float.PositiveInfinity;
            isAlarmActive = true;
            // Re-alert enemies
            AlertNearbyEnemies(alarmPosition);
            return;
        }

        // Alarm ještě neběžel – spusť vizuální efekty bez časovače
        alarmActive = true;
        isAlarmActive = true;
        OnAlarmTriggered?.Invoke();

        // Nepoužíváme AlarmSequence (má časovač) – jen efekty
        alarmCoroutine = StartCoroutine(PermanentAlarmSequence(alarmPosition));

        if (config.enableFlashingLights)
            lightFlashCoroutine = StartCoroutine(FlashLightsCoroutine());

        if (audioSource != null)
            audioSource.Play();
    }

    /// <summary>
    /// Manually stop alarm (if needed for gameplay).
    /// Permanentní alarm (ServerCore hack success) nelze zastavit.
    /// </summary>
    public void StopAlarm()
    {
        if (!alarmActive) return;

        if (alarmPermanent)
        {
            if (config.debugLog)
                Debug.LogWarning("[SecurityAlarmSystem] Cannot stop permanent alarm!", this);
            return;
        }

        if (config.debugLog)
            Debug.Log("[SecurityAlarmSystem] Alarm stopped manually", this);

        StopAllAlarmEffects();
    }

    // === ALARM LOGIC ===

    private IEnumerator AlarmSequence(Vector3 alarmPosition)
    {
        // Alert nearby enemies
        AlertNearbyEnemies(alarmPosition);

        // Run alarm – alarmTimeRemaining může být stackován z venku
        while (alarmTimeRemaining > 0f)
        {
            timeRemaining = alarmTimeRemaining;
            alarmTimeRemaining -= Time.deltaTime;
            yield return null;
        }

        // Alarm ended naturally
        StopAllAlarmEffects();
    }

    /// <summary>
    /// Permanentní alarm – pouze alertuje nepřátele, vizuální efekty běží dokud alarmActive.
    /// Žádný časovač – FlashLightsCoroutine se ukončí sama přes alarmActive flag.
    /// </summary>
    private IEnumerator PermanentAlarmSequence(Vector3 alarmPosition)
    {
        AlertNearbyEnemies(alarmPosition);
        timeRemaining = float.PositiveInfinity;
        // Nic dalšího – efekty řídí FlashLightsCoroutine přes alarmActive
        yield break;
    }

    private void StopAllAlarmEffects()
    {
        alarmActive = false;
        alarmPermanent = false;
        isAlarmActive = false;
        alarmTimeRemaining = 0f;
        timeRemaining = 0f;

        // Stop coroutines
        if (alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
            alarmCoroutine = null;
        }

        if (lightFlashCoroutine != null)
        {
            StopCoroutine(lightFlashCoroutine);
            lightFlashCoroutine = null;
        }

        // Stop audio
        if (audioSource != null)
            audioSource.Stop();

        // Restore lights
        RestoreLights();

        OnAlarmEnded?.Invoke();

        if (config.debugLog)
            Debug.Log("[SecurityAlarmSystem] Alarm ended", this);
    }

    // === ENEMY ALERTING ===

    private void AlertNearbyEnemies(Vector3 alarmPosition)
    {
        EnemyStateMachine[] allEnemies = FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);

        foreach (var enemy in allEnemies)
        {
            float distance = Vector3.Distance(enemy.transform.position, alarmPosition);

            if (distance <= config.alertRadius)
            {
                // Nearby enemies - alert to alarm position
                AlertEnemy(enemy, alarmPosition);
            }
            else if (config.distantEnemiesKeepPatrolling)
            {
                // Distant enemies - keep patrolling (no change)
                continue;
            }
            else
            {
                // Distant enemies - also investigate (optional behavior)
                AlertEnemy(enemy, alarmPosition);
            }
        }

        if (config.debugLog)
            Debug.Log($"[SecurityAlarmSystem] Alerted {allEnemies.Length} enemies within {config.alertRadius}m", this);
    }

    private void AlertEnemy(EnemyStateMachine enemy, Vector3 alarmPosition)
    {
        // Check if enemy is already in critical state (chase, attack, catch)
        if (enemy.CurrentState is EnemyChaseState ||
            enemy.CurrentState is EnemyCatchState)
        {
            return; // Don't interrupt these states
        }

        // Transition to Alert state with alarm position
        enemy.SetState(new EnemyAlertState(enemy, alarmPosition));

        if (config.debugLog)
            Debug.Log($"[SecurityAlarmSystem] Alerted enemy: {enemy.name}", enemy);
    }

    // === VISUAL EFFECTS ===

    private void CacheLightSettings()
    {
        originalLightColors.Clear();
        originalLightIntensities.Clear();

        foreach (var light in roomLights)
        {
            if (light == null) continue;
            originalLightColors[light] = light.color;
            originalLightIntensities[light] = light.intensity;
        }
    }

    private IEnumerator FlashLightsCoroutine()
    {
        float flashInterval = 1f / config.flashSpeed;
        bool lightsOn = true;

        while (alarmActive)
        {
            // Toggle lights
            foreach (var light in roomLights)
            {
                if (light == null) continue;

                if (lightsOn)
                {
                    light.color = config.alarmColor;
                    light.intensity = config.lightIntensity;
                }
                else
                {
                    light.intensity = 0f;
                }
            }

            lightsOn = !lightsOn;
            yield return new WaitForSeconds(flashInterval);
        }
    }

    private void RestoreLights()
    {
        foreach (var light in roomLights)
        {
            if (light == null) continue;

            if (originalLightColors.TryGetValue(light, out Color color))
                light.color = color;

            if (originalLightIntensities.TryGetValue(light, out float intensity))
                light.intensity = intensity;
        }
    }

    // === DEBUG ===

    private void OnDrawGizmosSelected()
    {
        if (config == null || !config.debugDrawRadius) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, config.alertRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Alarm Radius: {config.alertRadius}m\n" +
            $"Duration: {config.alarmDuration}s\n" +
            $"Active: {alarmActive}"
        );
#endif
    }
}