using UnityEngine;

/// <summary>
/// ScriptableObject configuration for security alarm system.
/// Controls visual effects, duration, enemy alert radius.
/// </summary>
[CreateAssetMenu(fileName = "SecurityAlarmConfig", menuName = "Zero Trace/Security Alarm Config")]
public class SecurityAlarmConfig : ScriptableObject
{
    [Header("Alarm Duration")]
    [Tooltip("How long alarm stays active (seconds)")]
    [Range(5f, 60f)]
    public float alarmDuration = 15f;

    [Header("Visual Effects - Lights")]
    [Tooltip("Enable flashing red lights in room")]
    public bool enableFlashingLights = true;

    [Tooltip("Flash speed (flashes per second)")]
    [Range(0.5f, 5f)]
    public float flashSpeed = 2f;

    [Tooltip("Light intensity during alarm")]
    [Range(0f, 8f)]
    public float lightIntensity = 3f;

    [Tooltip("Light color during alarm")]
    public Color alarmColor = Color.red;

    [Header("Visual Effects - UI Overlay")]
    [Tooltip("Enable red screen edge vignette")]
    public bool enableScreenOverlay = true;

    [Tooltip("Max overlay alpha (0-1)")]
    [Range(0f, 0.5f)]
    public float overlayMaxAlpha = 0.3f;

    [Tooltip("Overlay pulse speed")]
    [Range(0.5f, 5f)]
    public float overlayPulseSpeed = 1.5f;

    [Header("Enemy Alert")]
    [Tooltip("Radius to alert enemies (meters)")]
    [Range(10f, 100f)]
    public float alertRadius = 20f;

    [Tooltip("Enemies outside radius enter this state")]
    public bool distantEnemiesKeepPatrolling = true;

    [Header("Audio (Optional)")]
    [Tooltip("Alarm sound clip")]
    public AudioClip alarmSound;

    [Tooltip("Alarm sound volume")]
    [Range(0f, 1f)]
    public float alarmVolume = 0.7f;

    [Header("Debug")]
    [Tooltip("Show alarm radius in Scene view")]
    public bool debugDrawRadius = true;

    [Tooltip("Log alarm events")]
    public bool debugLog = true;

    private void OnValidate()
    {
        alarmDuration = Mathf.Max(1f, alarmDuration);
        alertRadius = Mathf.Max(5f, alertRadius);
    }
}