using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-point detection system (4 raycasts instead of 1).
/// Checks head, torso, left hand, right hand separately.
/// Returns visibility score (0-4) for suspicion calculation.
/// Performance: Coroutine-based batch raycasts (0.2s interval).
/// SRP: Only handles vision detection, delegates suspicion to EnemySuspicionSystem.
/// </summary>
public class EnemyMultiPointVision : MonoBehaviour
{
    [Header("Player Body Points Setup")]
    [Tooltip("OPTION 1: Manually assign 4 transforms (Head, Torso, LeftHand, RightHand)")]
    [SerializeField] private Transform[] playerBodyPoints = new Transform[4];

    [Tooltip("OPTION 2: Auto-use PlayerBodyPointsHelper if player has it (recommended)")]
    [SerializeField] private bool usePlayerBodyPointsHelper = true;

    [Header("Detection Settings")]
    [Tooltip("Maximum vision range in meters")]
    [SerializeField] private float visionRange = 15f;

    [Tooltip("Field of view angle in degrees")]
    [SerializeField] private float visionAngle = 90f;

    [Tooltip("Layer mask for vision raycasts (obstacles that block vision)")]
    [SerializeField] private LayerMask visionObstacleMask;

    [Header("Performance")]
    [Tooltip("How often to check vision (seconds) - default 0.2s")]
    [SerializeField] private float checkInterval = 0.2f;

    [Header("Auto-Find Player Points")]
    [Tooltip("If true, automatically finds player body points by name on Start")]
    [SerializeField] private bool autoFindPlayerPoints = true;

    [Header("Debug")]
    [SerializeField] private int visiblePointsCount = 0;
    [SerializeField] private bool[] pointVisibility = new bool[4];
    [SerializeField] private bool canSeePlayer;

    // References
    private Transform eyePosition;
    private Transform playerTransform;
    private EnemySuspicionSystem suspicionSystem;
    private EnemyStateMachine stateMachine;

    // Events
    public event Action<int> OnVisibilityChanged; // 0-4 visible points
    public event Action<Vector3> OnPlayerSpotted; // First detection
    public event Action<Vector3> OnPlayerLostSight; // Lost all points

    // State
    private Coroutine visionCheckCoroutine;
    private bool wasVisible;

    // Public API
    public int VisiblePoints => visiblePointsCount;
    public bool CanSeePlayer => canSeePlayer;

    public void Initialize(EnemyStateMachine machine, Transform player)
    {
        stateMachine = machine;
        playerTransform = player;
        suspicionSystem = GetComponent<EnemySuspicionSystem>();

        // Find or create eye position
        eyePosition = transform.Find("EyePosition");
        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            eyePosition = eyeObj.transform;
        }

        // Auto-find player body points if enabled
        if (playerTransform != null)
        {
            // Try to use PlayerBodyPointsHelper first (recommended)
            if (usePlayerBodyPointsHelper)
            {
                var helper = playerTransform.GetComponent<PlayerBodyPointsHelper>();
                if (helper != null)
                {
                    playerBodyPoints = helper.GetBodyPoints();
                    Debug.Log($"[EnemyMultiPointVision] {gameObject.name} using PlayerBodyPointsHelper", this);
                }
                else if (autoFindPlayerPoints)
                {
                    Debug.LogWarning($"[EnemyMultiPointVision] {gameObject.name} PlayerBodyPointsHelper not found, using auto-find", this);
                    AutoFindPlayerBodyPoints();
                }
            }
            else if (autoFindPlayerPoints)
            {
                AutoFindPlayerBodyPoints();
            }
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
        var wait = new WaitForSeconds(checkInterval);

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
        for (int i = 0; i < playerBodyPoints.Length; i++)
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
            // First detection
            OnPlayerSpotted?.Invoke(playerTransform.position);
        }
        else if (!nowVisible && wasVisible)
        {
            // Lost all visibility
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

        // Check 1: Range
        if (distanceToPoint > visionRange)
            return false;

        // Check 2: FOV angle
        float angleToPoint = Vector3.Angle(transform.forward, directionToPoint);
        if (angleToPoint > visionAngle * 0.5f)
            return false;

        // Check 3: Raycast (obstacles)
        Vector3 rayStart = eyePosition.position + directionToPoint * 0.1f;
        float rayDistance = distanceToPoint - 0.1f;

        bool hitObstacle = Physics.Raycast(
            rayStart,
            directionToPoint,
            out RaycastHit hit,
            rayDistance,
            visionObstacleMask
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

    // === AUTO-FIND PLAYER BODY POINTS ===

    private void AutoFindPlayerBodyPoints()
    {
        if (playerTransform == null)
            return;

        // Try to find by standard names (adjust to your player hierarchy)
        string[] pointNames = { "Head", "Spine", "LeftHand", "RightHand" };
        string[] alternativeNames = { "head", "torso", "lefthand", "righthand" };

        for (int i = 0; i < 4; i++)
        {
            // Try primary name
            Transform found = FindChildRecursive(playerTransform, pointNames[i]);

            // Try alternative name
            if (found == null)
                found = FindChildRecursive(playerTransform, alternativeNames[i]);

            playerBodyPoints[i] = found;

            if (found != null)
            {
                Debug.Log($"[EnemyMultiPointVision] Auto-found body point [{i}]: {found.name}");
            }
            else
            {
                Debug.LogWarning($"[EnemyMultiPointVision] Could not find body point: {pointNames[i]}");
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
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

        int validPoints = 0;
        for (int i = 0; i < playerBodyPoints.Length; i++)
        {
            if (playerBodyPoints[i] != null)
                validPoints++;
        }

        if (validPoints == 0)
        {
            Debug.LogError("[EnemyMultiPointVision] No player body points assigned!");
            valid = false;
        }
        else if (validPoints < 4)
        {
            Debug.LogWarning($"[EnemyMultiPointVision] Only {validPoints}/4 body points assigned. Detection may be less accurate.");
        }

        return valid;
    }

    // === DEBUG GIZMOS ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (eyePosition == null)
            return;

        float halfAngle = visionAngle * 0.5f;

        // Draw FOV cone
        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Vector3 forward = transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * forward * visionRange;
        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * forward * visionRange;

        Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightEdge);
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftEdge);

        // Draw raycasts to each body point
        if (playerBodyPoints != null)
        {
            for (int i = 0; i < playerBodyPoints.Length; i++)
            {
                if (playerBodyPoints[i] == null)
                    continue;

                Gizmos.color = pointVisibility[i] ? Color.green : Color.red;
                Gizmos.DrawLine(eyePosition.position, playerBodyPoints[i].position);
                Gizmos.DrawWireSphere(playerBodyPoints[i].position, 0.15f);

                // Label
                UnityEditor.Handles.Label(
                    playerBodyPoints[i].position + Vector3.up * 0.2f,
                    $"Point {i}: {(pointVisibility[i] ? "✓" : "✗")}",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = pointVisibility[i] ? Color.green : Color.red },
                        fontSize = 9
                    }
                );
            }
        }

        // Summary label
        UnityEditor.Handles.Label(
            eyePosition.position + Vector3.up * 0.5f,
            $"Visible: {visiblePointsCount}/4 points\n" +
            $"Range: {visionRange:F1}m | FOV: {visionAngle:F0}°",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.cyan },
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
#endif
}