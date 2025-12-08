using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-point detection system (4 raycasts instead of 1).
/// Checks head, torso, left hand, right hand separately.
/// Returns visibility score (0-4) for suspicion calculation.
/// Performance: Coroutine-based batch raycasts.
/// SRP: Only handles vision detection, delegates suspicion to EnemySuspicionSystem.
/// ALL SETTINGS FROM SO - no Inspector overrides.
/// </summary>
public class EnemyMultiPointVision : MonoBehaviour
{
    [Header("Manual Setup (Required)")]
    [Tooltip("Player body points - assign manually (4 transforms: Head, Torso, LeftHand, RightHand)")]
    [SerializeField] private Transform[] playerBodyPoints = new Transform[4];

    // Events
    public event Action<int> OnVisibilityChanged; // 0-4 visible points
    public event Action<Vector3> OnPlayerSpotted; // First detection
    public event Action<Vector3> OnPlayerLostSight; // Lost all points

    // References (set by EnemyStateMachine)
    private Transform eyePosition;
    private Transform playerTransform;
    private EnemySuspicionSystem suspicionSystem;
    private EnemyConfig config;
    private SuspicionConfig suspicionConfig;

    // State
    private int visiblePointsCount = 0;
    private bool[] pointVisibility = new bool[4];
    private bool canSeePlayer;
    private bool wasVisible;

    // Coroutine
    private Coroutine visionCheckCoroutine;

    // Public API
    public int VisiblePoints => visiblePointsCount;
    public bool CanSeePlayer => canSeePlayer;

    public void Initialize(EnemyStateMachine machine, EnemyConfig enemyConfig, SuspicionConfig susConfig, Transform player)
    {
        config = enemyConfig;
        suspicionConfig = susConfig;
        playerTransform = player;
        suspicionSystem = machine.GetComponent<EnemySuspicionSystem>();

        // Find or create eye position
        eyePosition = transform.Find("EyePosition");
        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            eyePosition = eyeObj.transform;
        }

        // Validate setup
        if (!ValidateSetup())
        {
            Debug.LogError($"[EnemyMultiPointVision] {gameObject.name} setup invalid!", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (config != null && suspicionConfig != null)
            StartVisionChecks();
    }

    private void OnDisable()
    {
        StopVisionChecks();
    }

    // === VISION CHECK LOGIC ===

    private void StartVisionChecks()
    {
        StopVisionChecks();
        visionCheckCoroutine = StartCoroutine(VisionCheckCoroutine());
    }

    private void StopVisionChecks()
    {
        if (visionCheckCoroutine != null)
        {
            StopCoroutine(visionCheckCoroutine);
            visionCheckCoroutine = null;
        }
    }

    private IEnumerator VisionCheckCoroutine()
    {
        var wait = new WaitForSeconds(suspicionConfig.visionCheckInterval);

        while (true)
        {
            PerformVisionCheck();
            yield return wait;
        }
    }

    private void PerformVisionCheck()
    {
        if (playerTransform == null || eyePosition == null)
        {
            canSeePlayer = false;
            return;
        }

        // Reset counters
        visiblePointsCount = 0;

        // Check each body point
        for (int i = 0; i < 4; i++)
        {
            if (playerBodyPoints[i] == null)
            {
                pointVisibility[i] = false;
                continue;
            }

            bool visible = CheckPointVisibility(playerBodyPoints[i].position);
            pointVisibility[i] = visible;

            if (visible)
                visiblePointsCount++;
        }

        // Update overall visibility
        bool nowVisible = visiblePointsCount > 0;

        // Fire events for state changes
        if (nowVisible && !wasVisible)
        {
            OnPlayerSpotted?.Invoke(playerTransform.position);
        }
        else if (!nowVisible && wasVisible)
        {
            OnPlayerLostSight?.Invoke(playerTransform.position);
        }

        canSeePlayer = nowVisible;
        wasVisible = nowVisible;

        // Update suspicion system
        if (suspicionSystem != null)
        {
            suspicionSystem.SetPlayerVisible(canSeePlayer, visiblePointsCount);
        }

        // Fire visibility changed event
        OnVisibilityChanged?.Invoke(visiblePointsCount);
    }

    private bool CheckPointVisibility(Vector3 targetPoint)
    {
        Vector3 directionToPoint = (targetPoint - eyePosition.position).normalized;
        float distanceToPoint = Vector3.Distance(eyePosition.position, targetPoint);

        // Check 1: Range (from EnemyConfig)
        if (distanceToPoint > config.visionRange)
            return false;

        // Check 2: FOV angle (from EnemyConfig)
        float angleToPoint = Vector3.Angle(transform.forward, directionToPoint);
        if (angleToPoint > config.visionAngle * 0.5f)
            return false;

        // Check 3: Raycast (obstacles) - LayerMask from EnemyConfig
        Vector3 rayStart = eyePosition.position + directionToPoint * 0.1f;
        float rayDistance = distanceToPoint - 0.1f;

        bool hitObstacle = Physics.Raycast(
            rayStart,
            directionToPoint,
            out RaycastHit hit,
            rayDistance,
            config.visionObstacleMask
        );

        if (hitObstacle)
        {
            // Check if we hit player body part (should be visible)
            bool hitPlayer = hit.collider.CompareTag("Player") ||
                           hit.collider.transform.root == playerTransform.root;

            return hitPlayer;
        }

        // Clear line of sight
        return true;
    }

    // === VALIDATION ===

    private bool ValidateSetup()
    {
        bool valid = true;

        if (eyePosition == null)
        {
            Debug.LogError("[EnemyMultiPointVision] Eye position not set!");
            valid = false;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[EnemyMultiPointVision] Player transform not set!");
            valid = false;
        }

        if (config == null)
        {
            Debug.LogError("[EnemyMultiPointVision] EnemyConfig not set!");
            valid = false;
        }

        if (suspicionConfig == null)
        {
            Debug.LogError("[EnemyMultiPointVision] SuspicionConfig not set!");
            valid = false;
        }

        int validPoints = 0;
        for (int i = 0; i < playerBodyPoints.Length; i++)
        {
            if (playerBodyPoints[i] != null)
                validPoints++;
        }

        if (validPoints == 0)
        {
            Debug.LogError("[EnemyMultiPointVision] No player body points assigned! Assign 4 transforms manually in Inspector.");
        }
        else if (validPoints < 4)
        {
            Debug.LogWarning($"[EnemyMultiPointVision] Only {validPoints}/4 body points assigned. Detection may be less accurate.");
        }

        return valid;
    }
}