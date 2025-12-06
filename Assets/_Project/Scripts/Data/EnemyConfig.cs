using UnityEngine;

/// <summary>
/// ScriptableObject configuration for enemy AI behavior.
/// Single source of truth for all enemy settings.
/// Create via: Assets > Create > Zero Trace > Enemy Config
/// </summary>
[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Zero Trace/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Vision Settings")]
    [Tooltip("Field of view angle in degrees (60-120 recommended)")]
    [Range(30f, 180f)]
    public float visionAngle = 90f;

    [Tooltip("Maximum vision range in meters")]
    [Range(5f, 30f)]
    public float visionRange = 10f;

    [Tooltip("Layer mask for vision raycasts (obstacles that block vision)")]
    public LayerMask visionObstacleMask;

    [Tooltip("How often to perform vision checks (seconds)")]
    [Range(0.05f, 0.5f)]
    public float visionCheckInterval = 0.1f;

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

    [Header("Chase Settings")]
    [Tooltip("Distance to start attacking")]
    [Range(0.5f, 3f)]
    public float attackRange = 1.2f;

    [Tooltip("How close before catching player (instant game over)")]
    [Range(0.3f, 2f)]
    public float catchRange = 1.2f;

    [Tooltip("Time between attack attempts")]
    [Range(0.5f, 3f)]
    public float attackCooldown = 1f;

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

    [Header("Debug")]
    [Tooltip("Show vision cone in Scene view")]
    public bool debugVision = true;

    [Tooltip("Show state transitions in console")]
    public bool debugStates = true;

    [Tooltip("Show movement paths")]
    public bool debugMovement = false;

    [Header("Suspicion System (Gradual Detection)")]
    [Tooltip("Enable gradual suspicion system (recommended). If false, uses instant detection.")]
    public bool enableSuspicionSystem = true;

    [Tooltip("Suspicion increase rate per second (base)")]
    [Range(5f, 100f)]
    public float suspicionBuildRate = 20f;

    [Tooltip("Suspicion decrease rate per second when player hidden")]
    [Range(5f, 50f)]
    public float suspicionDecayRate = 10f;

    [Tooltip("Grace period before suspicion starts decaying (seconds)")]
    [Range(0f, 5f)]
    public float suspicionDecayGracePeriod = 1f;

    [Tooltip("Multiplier per visible body part (0.5 = +50% per part, max 4 parts)")]
    [Range(0f, 2f)]
    public float suspicionVisibilityMultiplier = 0.5f;

    [Tooltip("Suspicion % to trigger Alert state (recommended 30%)")]
    [Range(10f, 99f)]
    public float suspicionAlertThreshold = 30f;

    [Tooltip("Suspicion % to trigger Chase state (always 100%)")]
    public float suspicionChaseThreshold = 100f;

    [Header("Multi-Point Vision (Enhanced Detection)")]
    [Tooltip("Enable 4-point body detection (head/torso/hands) instead of single raycast")]
    public bool enableMultiPointVision = true;

    [Tooltip("How often to check vision (seconds) - lower = more responsive but heavier")]
    [Range(0.05f, 0.5f)]
    public float visionCheckIntervalMultiPoint = 0.2f;

    private void OnValidate()
    {
        // Ensure logical values
        if (chaseSpeed <= patrolSpeed)
        {
            Debug.LogWarning($"[EnemyConfig] Chase speed ({chaseSpeed}) should be > patrol speed ({patrolSpeed})");
        }

        if (catchRange > attackRange)
        {
            Debug.LogWarning($"[EnemyConfig] Catch range ({catchRange}) should be <= attack range ({attackRange})");
        }

        if (visionRange < attackRange)
        {
            Debug.LogWarning($"[EnemyConfig] Vision range ({visionRange}) should be >= attack range ({attackRange})");
        }
    }
}