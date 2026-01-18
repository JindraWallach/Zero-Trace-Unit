using UnityEngine;

/// <summary>
/// ScriptableObject configuration for enemy AI behavior.
/// Single source of truth for all enemy settings.
/// Create via: Assets > Create > Zero Trace > Enemy Config
/// </summary>
[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Zero Trace/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Suspicion System")]
    [Tooltip("Enable gradual suspicion detection (recommended)")]
    public bool enableSuspicionSystem = true;

    [Tooltip("Suspicion configuration (separate SO for modularity)")]
    public SuspicionConfig suspicionConfig;

    [Header("Vision Settings")]
    [Tooltip("Field of view angle in degrees (60-120 recommended)")]
    [Range(30f, 180f)]
    public float visionAngle = 90f;

    [Tooltip("Maximum vision range in meters")]
    [Range(5f, 30f)]
    public float visionRange = 10f;

    [Tooltip("Layer mask for vision raycasts (obstacles that block vision)")]
    public LayerMask visionObstacleMask;

    [Header("Movement Settings")]
    [Tooltip("Patrol walking speed")]
    [Range(0.5f, 3f)]
    public float patrolSpeed = 1.5f;

    [Tooltip("Chase running speed")]
    [Range(2f, 8f)]
    public float chaseSpeed = 4f;

    [Tooltip("How close to waypoint before moving to next")]
    [Range(0.1f, 2f)]
    public float waypointReachDistance = 0.5f;

    [Tooltip("How long to wait at each waypoint (seconds)")]
    [Range(0f, 10f)]
    public float waypointWaitTime = 2f;

    [Header("Alert Settings")]
    [Tooltip("How long enemy stays alert after losing player")]
    [Range(1f, 10f)]
    public float alertDuration = 3f;

    [Tooltip("Time before transitioning from Alert to Search")]
    [Range(0.5f, 5f)]
    public float alertToSearchDelay = 1.5f;

    [Tooltip("How close before catching player (instant game over)")]
    [Range(0.3f, 2f)]
    public float catchRange = 1.2f;

    [Header("Search Settings")]
    [Tooltip("How long to search last known position")]
    [Range(3f, 15f)]
    public float searchDuration = 8f;

    [Tooltip("Radius to search around last known position")]
    [Range(2f, 10f)]
    public float searchRadius = 5f;

    [Tooltip("Movement speed while searching")]
    [Range(1f, 4f)]
    public float searchSpeed = 2f;

    [Header("Memory Settings")]
    [Tooltip("How long enemy remembers player position after losing sight")]
    [Range(1f, 10f)]
    public float memoryDuration = 5f;

    [Header("Catch Settings")]
    [Tooltip("Force magnitude applied to player ragdoll on catch")]
    [Range(1f, 100f)]
    public float catchForceMagnitude = 800f;

    [Tooltip("Vertical force component (0 = horizontal, 1 = upward)")]
    [Range(0f, 1f)]
    public float catchForceVertical = 0.2f;

    [Tooltip("Delay after taser hit before applying force (visual timing)")]
    [Range(0f, 0.5f)]
    public float taserHitDelay = 0.15f;

    [Header("Debug")]
    [Tooltip("Show vision cone in Scene view")]
    public bool debugVision = true;

    [Tooltip("Show state transitions in console")]
    public bool debugStates = true;

    [Tooltip("Show movement paths")]
    public bool debugMovement = false;

    private void OnValidate()
    {
        // Ensure logical values
        if (chaseSpeed <= patrolSpeed)
        {
            Debug.LogWarning($"[EnemyConfig] Chase speed ({chaseSpeed}) should be > patrol speed ({patrolSpeed})");
        }

        // Warn if suspicion enabled but config missing
        if (enableSuspicionSystem && suspicionConfig == null)
        {
            Debug.LogWarning($"[EnemyConfig] Suspicion system enabled but SuspicionConfig not assigned!", this);
        }
    }
}