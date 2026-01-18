using UnityEngine;

/// <summary>
/// ScriptableObject configuration for security camera behavior.
/// Single source of truth for all camera settings.
/// Create via: Assets > Create > Zero Trace > Security Camera Config
/// </summary>
[CreateAssetMenu(fileName = "SecurityCameraConfig", menuName = "Zero Trace/Security Camera Config")]
public class SecurityCameraConfig : ScriptableObject
{
    [Header("Vision Settings")]
    [Tooltip("Maximum vision range in meters")]
    [Range(5f, 30f)]
    public float visionRange = 15f;

    [Tooltip("Field of view angle in degrees")]
    [Range(30f, 120f)]
    public float visionAngle = 60f;

    [Tooltip("How often to check for player visibility (seconds)")]
    [Range(0.1f, 1f)]
    public float visionCheckInterval = 0.3f;

    [Tooltip("Layer mask for vision raycasts (obstacles that block vision)")]
    public LayerMask visionObstacleMask;

    [Header("Suspicion Settings")]
    [Tooltip("Time to reach 100% suspicion when player visible (seconds)")]
    [Range(0.5f, 5f)]
    public float suspicionBuildTime = 1.5f;

    [Tooltip("Time to decay from 100% to 0% when player hidden (seconds)")]
    [Range(1f, 10f)]
    public float suspicionDecayTime = 3f;

    [Header("Alert Settings")]
    [Tooltip("Delay before triggering alarm after reaching 100% suspicion")]
    [Range(0f, 2f)]
    public float alertDelay = 0.5f;

    [Tooltip("Cooldown before camera can alert again (seconds)")]
    [Range(5f, 30f)]
    public float alertCooldown = 10f;

    [Header("Debug")]
    [Tooltip("Show vision cone in Scene view")]
    public bool debugVision = true;

    [Tooltip("Show state transitions in console")]
    public bool debugStates = true;

    private void OnValidate()
    {
        visionRange = Mathf.Max(1f, visionRange);
        visionAngle = Mathf.Clamp(visionAngle, 10f, 180f);
        visionCheckInterval = Mathf.Clamp(visionCheckInterval, 0.05f, 1f);
        suspicionBuildTime = Mathf.Max(0.1f, suspicionBuildTime);
        suspicionDecayTime = Mathf.Max(0.1f, suspicionDecayTime);
    }
}