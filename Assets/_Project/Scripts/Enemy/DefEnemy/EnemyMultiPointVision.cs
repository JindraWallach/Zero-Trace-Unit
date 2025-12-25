using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-point detection system (4 raycasts instead of 1).
/// Checks head, torso, left hand, right hand separately.
/// Returns visibility score (0-4) for suspicion calculation.
/// FIXED: Přidány gizmos z EnemyVisionDetector + opravená detekce.
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

    private Coroutine visionCheckCoroutine;

    // Debug visualization data
    private Vector3[] lastRayStarts = new Vector3[4];
    private Vector3[] lastRayDirections = new Vector3[4];
    private bool[] lastRaycastHits = new bool[4];
    private RaycastHit[] lastHitInfo = new RaycastHit[4];
    private float[] lastAngles = new float[4];
    private float[] lastDistances = new float[4];

    private void OnDestroy()
    {
        // Unregister from manager
        EnemyDetectionManager.Instance?.UnregisterDetector(this);
    }

    // Public API
    public int VisiblePoints => visiblePointsCount;
    public bool CanSeePlayer => canSeePlayer;

    // Compatibility wrapper for legacy manager call
    public void PerformDetectionCheck()
    {
        PerformVisionCheck();
    }

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

        // Register with detection manager for batch processing
        EnemyDetectionManager.Instance?.RegisterDetector(this);

        Debug.Log($"[EnemyMultiPointVision] {gameObject.name} initialized. Eye at {eyePosition.position}, Player: {playerTransform.name}", this);
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
        Debug.Log($"[EnemyMultiPointVision] {gameObject.name} started vision checks (interval: {suspicionConfig.visionCheckInterval}s)", this);
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
                lastRaycastHits[i] = false;
                continue;
            }

            bool visible = CheckPointVisibility(i, playerBodyPoints[i].position);
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
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name} SPOTTED player! Visible parts: {visiblePointsCount}/4", this);
        }
        else if (!nowVisible && wasVisible)
        {
            OnPlayerLostSight?.Invoke(playerTransform.position);
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name} LOST sight of player", this);
        }

        canSeePlayer = nowVisible;
        wasVisible = nowVisible;

        // Update suspicion system
        if (suspicionSystem != null)
        {
            suspicionSystem.SetPlayerVisible(canSeePlayer, visiblePointsCount);
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name} -> SuspicionSystem.SetPlayerVisible called. Visible={canSeePlayer}, Parts={visiblePointsCount}", this);
        }
        else
        {
            Debug.LogWarning($"[EnemyMultiPointVision] {gameObject.name} suspicionSystem is NULL when trying to update suspicion", this);
        }

        // Fire visibility changed event
        OnVisibilityChanged?.Invoke(visiblePointsCount);

        if (config.debugStates)
        {
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name} Vision check: {visiblePointsCount}/4 parts visible. CanSee: {canSeePlayer}", this);
        }
    }

    private bool CheckPointVisibility(int pointIndex, Vector3 targetPoint)
    {
        Vector3 directionToPoint = (targetPoint - eyePosition.position).normalized;
        float distanceToPoint = Vector3.Distance(eyePosition.position, targetPoint);

        // Store for debug
        lastRayDirections[pointIndex] = directionToPoint;
        lastDistances[pointIndex] = distanceToPoint;

        // Check 1: Range (from EnemyConfig)
        if (distanceToPoint > config.visionRange)
        {
            lastRaycastHits[pointIndex] = false;
            return false;
        }

        // Check 2: FOV angle (from EnemyConfig)
        float angleToPoint = Vector3.Angle(transform.forward, directionToPoint);
        lastAngles[pointIndex] = angleToPoint;

        if (angleToPoint > config.visionAngle * 0.5f)
        {
            lastRaycastHits[pointIndex] = false;
            return false;
        }

        // Check 3: Raycast (obstacles) - LayerMask from EnemyConfig
        Vector3 rayStart = eyePosition.position + directionToPoint * 0.1f;
        float rayDistance = distanceToPoint - 0.1f;

        lastRayStarts[pointIndex] = rayStart;

        bool hitObstacle = Physics.Raycast(
            rayStart,
            directionToPoint,
            out RaycastHit hit,
            rayDistance,
            config.visionObstacleMask
        );

        lastRaycastHits[pointIndex] = hitObstacle;
        lastHitInfo[pointIndex] = hit;

        if (hitObstacle)
        {
            // Check if we hit player body part (should be visible)
            bool hitPlayer = hit.collider.CompareTag("Player") ||
                           hit.collider.transform.root == playerTransform.root;

            if (config.debugStates)
            {
                Debug.Log($"[EnemyMultiPointVision] Point {pointIndex}: Raycast hit {hit.collider.name} " +
                         $"(IsPlayer: {hitPlayer}, Dist: {hit.distance:F2}m)", this);
            }

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

    // === GIZMOS (PŘENESENO Z EnemyVisionDetector) ===

    private void OnDrawGizmosSelected()
    {
        if (config != null && config.debugVision)
        {
            DrawVisionGizmos();
        }
    }

    private void OnDrawGizmos()
    {
        if (config != null && config.debugVision)
        {
            DrawVisionGizmos();
        }
    }

    private void DrawVisionGizmos()
    {
        if (eyePosition == null || config == null)
            return;

        float halfAngle = config.visionAngle * 0.5f;
        float range = config.visionRange;

        // === 1. Eye Position ===
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eyePosition.position, 0.15f);

        // === 2. Forward Direction (BLUE) ===
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(eyePosition.position, transform.forward * range);
        // Draw arrow head for forward
        Vector3 forwardEnd = eyePosition.position + transform.forward * range;
        Vector3 arrowRight = Quaternion.Euler(0, 20, 0) * -transform.forward * 0.5f;
        Vector3 arrowLeft = Quaternion.Euler(0, -20, 0) * -transform.forward * 0.5f;
        Gizmos.DrawLine(forwardEnd, forwardEnd + arrowRight);
        Gizmos.DrawLine(forwardEnd, forwardEnd + arrowLeft);

        // === 3. Vision Cone Edges (YELLOW or RED) ===
        Vector3 forward = transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * forward * range;
        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * forward * range;

        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightEdge);
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftEdge);

        // Draw arc
        Vector3 prevPoint = eyePosition.position + rightEdge;
        for (int i = 1; i <= 20; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / 20f);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 point = eyePosition.position + direction * range;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        // === 4. Multi-Point Raycasts (4 body parts) ===
        if (playerTransform != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (playerBodyPoints[i] == null)
                    continue;

                Vector3 targetPos = playerBodyPoints[i].position;
                bool isVisible = pointVisibility[i];

                // Draw line to body part
                Gizmos.color = isVisible ? Color.green : new Color(1f, 0f, 1f, 0.3f); // Green if visible, transparent magenta if not
                Gizmos.DrawLine(eyePosition.position, targetPos);

                // Draw body part marker
                Gizmos.color = isVisible ? Color.green : Color.red;
                Gizmos.DrawWireSphere(targetPos, 0.15f);

                // Draw raycast visualization
                if (lastRayStarts[i] != Vector3.zero)
                {
                    if (lastRaycastHits[i])
                    {
                        // Hit something
                        Vector3 hitPoint = lastHitInfo[i].point;

                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(lastRayStarts[i], hitPoint);
                        Gizmos.DrawWireSphere(hitPoint, 0.08f);

                        // Normal
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(hitPoint, hitPoint + lastHitInfo[i].normal * 0.3f);

#if UNITY_EDITOR
                        // Label
                        UnityEditor.Handles.Label(
                            hitPoint + Vector3.up * 0.2f,
                            $"Part{i}: BLOCKED\n{lastHitInfo[i].collider.name}",
                            new GUIStyle()
                            {
                                normal = new GUIStyleState() { textColor = Color.red },
                                fontSize = 10,
                                fontStyle = FontStyle.Bold
                            }
                        );
#endif
                    }
                    else
                    {
                        // Clear line
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(lastRayStarts[i], targetPos);
                    }

                    // Raycast start point
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(lastRayStarts[i], 0.06f);
                }
            }

            // === 5. Overall status label ===
#if UNITY_EDITOR
            Vector3 labelPos = eyePosition.position + Vector3.up * 0.5f;
            string statusText = $"MULTI-POINT VISION\n" +
                               $"Visible: {visiblePointsCount}/4 parts\n" +
                               $"CanSee: {(canSeePlayer ? "YES" : "NO")}\n" +
                               $"Range: {config.visionRange:F1}m\n" +
                               $"FOV: {config.visionAngle:F0}°";

            UnityEditor.Handles.Label(
                labelPos,
                statusText,
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = canSeePlayer ? Color.green : Color.yellow },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                }
            );
#endif
        }
    }
}