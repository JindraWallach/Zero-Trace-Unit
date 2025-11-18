using System;
using UnityEngine;

/// <summary>
/// FOV (Field of View) detection system for enemy vision.
/// Uses raycast-based line-of-sight checks with angle/range constraints.
/// Registers with EnemyDetectionManager for batch processing optimization.
/// </summary>
public class EnemyVisionDetector : MonoBehaviour
{
    private EnemyStateMachine machine;
    private Transform eyePosition;

    // Detection state
    private bool canSeePlayer;
    private Vector3 lastSeenPlayerPosition;
    private float timeSinceLastSeen;

    // Events
    public event Action<Vector3> OnPlayerSpotted;
    public event Action<Vector3> OnPlayerLostSight;

    // Public accessors
    public bool CanSeePlayerNow => canSeePlayer;
    public Vector3 LastSeenPosition => lastSeenPlayerPosition;

    public void Initialize(EnemyStateMachine stateMachine)
    {
        machine = stateMachine;

        // Find or create eye position (for raycast origin)
        eyePosition = transform.Find("EyePosition");
        if (eyePosition == null)
        {
            // Create eye position at head height if not found
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Head height
            eyePosition = eyeObj.transform;
        }

        // Register with batch detection manager
        EnemyDetectionManager.Instance?.RegisterDetector(this);
    }

    private void OnDestroy()
    {
        // Unregister from manager
        EnemyDetectionManager.Instance?.UnregisterDetector(this);
    }

    /// <summary>
    /// Main detection check - called by EnemyDetectionManager in batches.
    /// Returns true if player is visible.
    /// </summary>
    public bool PerformDetectionCheck()
    {
        if (machine.PlayerTransform == null)
            return false;

        Vector3 playerPosition = machine.PlayerTransform.position;
        Vector3 directionToPlayer = (playerPosition - eyePosition.position).normalized;
        float distanceToPlayer = Vector3.Distance(eyePosition.position, playerPosition);

        // Check 1: Range check (early out)
        if (distanceToPlayer > machine.Config.visionRange)
        {
            HandlePlayerLost();
            return false;
        }

        // Check 2: Angle check (FOV cone)
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > machine.Config.visionAngle * 0.5f)
        {
            HandlePlayerLost();
            return false;
        }

        // Check 3: Line-of-sight raycast (obstacles blocking vision)
        RaycastHit hit;
        if (Physics.Raycast(eyePosition.position, directionToPlayer, out hit, distanceToPlayer, machine.Config.visionObstacleMask))
        {
            // Something is blocking view
            HandlePlayerLost();
            return false;
        }

        // Player is visible!
        HandlePlayerSpotted(playerPosition);
        return true;
    }

    /// <summary>
    /// Overload for states to check vision manually (with out parameter).
    /// </summary>
    public bool CanSeePlayer(out Vector3 playerPosition)
    {
        playerPosition = canSeePlayer ? lastSeenPlayerPosition : Vector3.zero;
        return canSeePlayer;
    }

    /// <summary>
    /// Check if a specific position is visible (for investigating sounds/alerts).
    /// </summary>
    public bool CanSeePosition(Vector3 position)
    {
        Vector3 directionToPosition = (position - eyePosition.position).normalized;
        float distance = Vector3.Distance(eyePosition.position, position);

        // Range check
        if (distance > machine.Config.visionRange)
            return false;

        // Angle check
        float angle = Vector3.Angle(transform.forward, directionToPosition);
        if (angle > machine.Config.visionAngle * 0.5f)
            return false;

        // Raycast check
        RaycastHit hit;
        if (Physics.Raycast(eyePosition.position, directionToPosition, out hit, distance, machine.Config.visionObstacleMask))
        {
            return false; // Blocked
        }

        return true;
    }

    /// <summary>
    /// Get direction to player (for aiming/facing).
    /// </summary>
    public Vector3 GetDirectionToPlayer()
    {
        if (machine.PlayerTransform == null)
            return transform.forward;

        return (machine.PlayerTransform.position - transform.position).normalized;
    }

    // === Event Handlers ===

    private void HandlePlayerSpotted(Vector3 position)
    {
        bool wasNotSeeing = !canSeePlayer;

        canSeePlayer = true;
        lastSeenPlayerPosition = position;
        timeSinceLastSeen = 0f;

        // Fire event only on first detection (not every frame)
        if (wasNotSeeing)
        {
            OnPlayerSpotted?.Invoke(position);

            if (machine.Config.debugStates)
                Debug.Log($"[EnemyVision] {gameObject.name} spotted player at {position}", this);
        }
    }

    private void HandlePlayerLost()
    {
        if (!canSeePlayer)
            return; // Already lost

        canSeePlayer = false;
        Vector3 lastPos = lastSeenPlayerPosition;

        OnPlayerLostSight?.Invoke(lastPos);

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyVision] {gameObject.name} lost sight of player at {lastPos}", this);
    }

    // === Debug Visualization ===

    public void DrawVisionGizmos()
    {
        if (machine == null || eyePosition == null)
            return;

        // Draw vision cone
        float halfAngle = machine.Config.visionAngle * 0.5f;
        float range = machine.Config.visionRange;

        // Vision cone edges
        Vector3 forward = transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * forward * range;
        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * forward * range;

        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;

        // Draw cone
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

        // Draw line to player if visible
        if (canSeePlayer && machine.PlayerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePosition.position, machine.PlayerTransform.position);
        }

        // Draw eye position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eyePosition.position, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (machine != null && machine.Config != null && machine.Config.debugVision)
        {
            DrawVisionGizmos();
        }
    }
}