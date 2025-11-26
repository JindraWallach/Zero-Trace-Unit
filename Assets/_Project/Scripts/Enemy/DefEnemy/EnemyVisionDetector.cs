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

    // Debug info
    private float lastAngleToPlayer;
    private Vector3 lastDirectionToPlayer;
    private RaycastHit lastRaycastHit;
    private bool lastRaycastHitSomething;
    private Vector3 lastRayStart;

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

        // Store for debug visualization
        lastDirectionToPlayer = directionToPlayer;

        // Check 1: Range check (early out)
        if (distanceToPlayer > machine.Config.visionRange)
        {
            HandlePlayerLost();
            return false;
        }

        //Debug.Log($"[EnemyVision] {gameObject.name} checking vision to player at distance {distanceToPlayer}", this);

        // Check 2: Angle check (FOV cone)
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        lastAngleToPlayer = angleToPlayer; // Store for debug

        if (machine.Config.debugStates)
        {
            Debug.Log($"[EnemyVision] {gameObject.name} Angle Check:\n" +
                      $"  Transform.forward: {transform.forward}\n" +
                      $"  Direction to player: {directionToPlayer}\n" +
                      $"  Angle: {angleToPlayer:F1}° (limit: {machine.Config.visionAngle * 0.5f:F1}°)\n" +
                      $"  Inside FOV: {angleToPlayer <= machine.Config.visionAngle * 0.5f}", this);
        }

        if (angleToPlayer > machine.Config.visionAngle * 0.5f)
        {
            HandlePlayerLost();
            return false;
        }

        // Check 3: Line-of-sight raycast (obstacles blocking vision)
        RaycastHit hit;
        // Start slightly in front of eye position to avoid self-collision
        Vector3 rayStart = eyePosition.position + directionToPlayer * 0.1f;
        float rayDistance = distanceToPlayer - 0.1f;

        // Store for debug visualization
        lastRayStart = rayStart;
        lastRaycastHitSomething = Physics.Raycast(rayStart, directionToPlayer, out hit, rayDistance, machine.Config.visionObstacleMask);
        lastRaycastHit = hit;

        if (lastRaycastHitSomething)
        {
            // Check if we hit the player (should be visible) or an obstacle
            bool hitPlayer = hit.collider.CompareTag("Player") || hit.collider.transform == machine.PlayerTransform;

            if (machine.Config.debugStates)
            {
                Debug.Log($"[EnemyVision] {gameObject.name} Raycast hit: {hit.collider.name} " +
                          $"(Tag: {hit.collider.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, " +
                          $"IsPlayer: {hitPlayer}, Distance: {hit.distance:F2})", this);
            }

            if (!hitPlayer)
            {
                // Something is blocking view
                HandlePlayerLost();
                return false;
            }
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

        float halfAngle = machine.Config.visionAngle * 0.5f;
        float range = machine.Config.visionRange;

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

        // === 4. Direction to Player + RAYCAST VISUALIZATION ===
        if (machine.PlayerTransform != null)
        {
            Vector3 playerPos = machine.PlayerTransform.position;
            Vector3 dirToPlayer = (playerPos - eyePosition.position).normalized;
            float distToPlayer = Vector3.Distance(eyePosition.position, playerPos);

            // Draw direction line to player (thin, reference line)
            float drawDist = Mathf.Min(distToPlayer, range);
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Transparent magenta
            Gizmos.DrawLine(eyePosition.position, eyePosition.position + dirToPlayer * drawDist);

            // === RAYCAST VISUALIZATION ===
            if (lastRayStart != Vector3.zero)
            {
                if (lastRaycastHitSomething)
                {
                    // Raycast HIT something
                    Vector3 hitPoint = lastRaycastHit.point;

                    // Draw ray to hit point (RED = blocked)
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(lastRayStart, hitPoint);

                    // Draw hit point and normal
                    Gizmos.DrawWireSphere(hitPoint, 0.1f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(hitPoint, hitPoint + lastRaycastHit.normal * 0.5f);

                    // Draw dotted line from hit to player (what we can't see)
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    DrawDottedLine(hitPoint, playerPos, 0.2f);

#if UNITY_EDITOR
                    // Label showing what blocked the view
                    UnityEditor.Handles.Label(
                        hitPoint + Vector3.up * 0.3f,
                        $"BLOCKED BY:\n{lastRaycastHit.collider.name}\n" +
                        $"Layer: {LayerMask.LayerToName(lastRaycastHit.collider.gameObject.layer)}\n" +
                        $"Dist: {lastRaycastHit.distance:F2}m",
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
                    // Raycast CLEAR (nothing blocking)
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(lastRayStart, playerPos);
                }

                // Draw raycast start point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(lastRayStart, 0.08f);
            }

            // Draw player position marker
            Gizmos.color = canSeePlayer ? Color.green : Color.red;
            Gizmos.DrawWireSphere(playerPos, 0.3f);

            // === 5. Draw angle arc between forward and direction to player ===
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            bool insideFOV = angleToPlayer <= halfAngle;

            // Draw arc showing the angle
            Gizmos.color = insideFOV ? Color.green : Color.red;
            Vector3 arcStart = eyePosition.position;
            float arcRadius = 2f; // Visual radius for the arc

            // Determine rotation direction (left or right of forward)
            Vector3 cross = Vector3.Cross(transform.forward, dirToPlayer);
            float sign = Mathf.Sign(cross.y);

            // Draw arc from forward to direction
            int arcSegments = 10;
            Vector3 prevArcPoint = arcStart + transform.forward * arcRadius;
            for (int i = 1; i <= arcSegments; i++)
            {
                float currentAngle = Mathf.Lerp(0, angleToPlayer * sign, i / (float)arcSegments);
                Vector3 arcDir = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
                Vector3 arcPoint = arcStart + arcDir * arcRadius;
                Gizmos.DrawLine(prevArcPoint, arcPoint);
                prevArcPoint = arcPoint;
            }

            // Draw text info (if in editor)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                eyePosition.position + Vector3.up * 0.5f,
                $"Angle: {angleToPlayer:F1}° / {halfAngle:F1}°\n" +
                $"FOV: {(insideFOV ? "✓" : "✗")}\n" +
                $"Dist: {distToPlayer:F1}m / {range:F1}m\n" +
                $"Visible: {(canSeePlayer ? "YES" : "NO")}"
            );
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (machine != null && machine.Config != null && machine.Config.debugVision)
        {
            DrawVisionGizmos();
        }
    }

    // Optional: Always draw when debug is on
    private void OnDrawGizmos()
    {
        if (machine != null && machine.Config != null && machine.Config.debugVision)
        {
            DrawVisionGizmos();
        }
    }

    // Helper method to draw dotted lines
    private void DrawDottedLine(Vector3 start, Vector3 end, float segmentLength)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        int segments = Mathf.CeilToInt(distance / segmentLength);

        for (int i = 0; i < segments; i += 2)
        {
            Vector3 segStart = start + direction * (i * segmentLength);
            Vector3 segEnd = start + direction * Mathf.Min((i + 1) * segmentLength, distance);
            Gizmos.DrawLine(segStart, segEnd);
        }
    }
}