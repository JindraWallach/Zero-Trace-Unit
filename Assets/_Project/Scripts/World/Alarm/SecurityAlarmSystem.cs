using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Security alarm system triggered by failed hacking attempts.
/// Handles visual effects and enemy alerting.
/// SRP: Only manages alarm state and effects.
/// Performance: Coroutine-based, no Update().
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
    private Coroutine alarmCoroutine;
    private Coroutine lightFlashCoroutine;
    private AudioSource audioSource;

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
    /// Trigger alarm at specific position (door location).
    /// </summary>
    public void TriggerAlarm(Vector3 alarmPosition)
    {
        if (alarmActive)
        {
            if (config.debugLog)
                Debug.LogWarning("[SecurityAlarmSystem] Alarm already active!", this);
            return;
        }

        if (config.debugLog)
            Debug.Log($"[SecurityAlarmSystem] ALARM TRIGGERED at {alarmPosition}", this);

        alarmActive = true;
        OnAlarmTriggered?.Invoke();

        // Start alarm effects
        alarmCoroutine = StartCoroutine(AlarmSequence(alarmPosition));

        if (config.enableFlashingLights)
            lightFlashCoroutine = StartCoroutine(FlashLightsCoroutine());

        if (audioSource != null)
            audioSource.Play();
    }

    /// <summary>
    /// Manually stop alarm (if needed for gameplay).
    /// </summary>
    public void StopAlarm()
    {
        if (!alarmActive) return;

        if (config.debugLog)
            Debug.Log("[SecurityAlarmSystem] Alarm stopped manually", this);

        StopAllAlarmEffects();
    }

    // === ALARM LOGIC ===

    private IEnumerator AlarmSequence(Vector3 alarmPosition)
    {
        float elapsed = 0f;

        // Alert nearby enemies
        AlertNearbyEnemies(alarmPosition);

        // Run alarm for duration
        while (elapsed < config.alarmDuration)
        {
            timeRemaining = config.alarmDuration - elapsed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Alarm ended naturally
        StopAllAlarmEffects();
    }

    private void StopAllAlarmEffects()
    {
        alarmActive = false;
        isAlarmActive = false;
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
        // Check if enemy is already in critical state (chase, catch)
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