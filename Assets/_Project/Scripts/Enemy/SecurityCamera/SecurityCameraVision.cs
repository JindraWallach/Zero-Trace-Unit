using UnityEngine;

/// <summary>
/// Simplified FOV detection for security cameras.
/// Timer-based checks for performance (no batch manager needed).
/// </summary>
public class SecurityCameraVision : MonoBehaviour
{
    public Transform eyePosition;

    private SecurityCameraConfig config;
    private Transform player;

    // Detection state
    private bool lastResult;
    private float lastCheckTime;
    private const float CHECK_INTERVAL = 0.3f;

    // Debug
    private Vector3 lastDirectionToPlayer;
    private float lastAngleToPlayer;
    private bool lastRaycastHit;
    private RaycastHit lastHitInfo;

    public void Initialize(SecurityCameraConfig cameraConfig, Transform playerTransform)
    {
        config = cameraConfig;
        player = playerTransform;
        lastResult = false;
    }

    /// <summary>
    /// Check if player is visible (cached with timer).
    /// </summary>
    /// <summary>
    /// Check if player is visible (cached with timer).
    /// </summary>
    public bool CanSeePlayer()
    {
        // Timer-based check for performance (uses config interval)
        if (Time.time - lastCheckTime < config.visionCheckInterval)
            return lastResult;

        lastCheckTime = Time.time;
        lastResult = PerformVisionCheck();
        return lastResult;
    }

    private bool PerformVisionCheck()
    {
        if (player == null || eyePosition == null)
            return false;

        // Target player center (not feet)
        Vector3 playerCenter = player.position + Vector3.up * 1f; // Adjust height as needed
        Vector3 directionToPlayer = (playerCenter - eyePosition.position).normalized;
        float distanceToPlayer = Vector3.Distance(eyePosition.position, playerCenter);

        lastDirectionToPlayer = directionToPlayer;

        // Check 1: Range
        if (distanceToPlayer > config.visionRange)
        {
            if (config.debugStates)
                Debug.Log($"[SecurityCameraVision] {name} - Out of range: {distanceToPlayer:F2}m > {config.visionRange}m");
            return false;
        }

        // Check 2: Angle (FOV cone)
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        lastAngleToPlayer = angleToPlayer;

        if (angleToPlayer > config.visionAngle * 0.5f)
        {
            if (config.debugStates)
                Debug.Log($"[SecurityCameraVision] {name} - Outside FOV: {angleToPlayer:F1}° > {config.visionAngle * 0.5f:F1}°");
            return false;
        }

        // Check 3: Raycast (obstacles)
        Vector3 rayStart = eyePosition.position;
        float rayDistance = distanceToPlayer;

        lastRaycastHit = Physics.Raycast(rayStart, directionToPlayer, out lastHitInfo, rayDistance, config.visionObstacleMask);

        if (lastRaycastHit)
        {
            // Check if hit player or obstacle
            bool hitPlayer = lastHitInfo.collider.CompareTag("Player") ||
                            lastHitInfo.collider.transform.root == player.root;

            if (config.debugStates)
            {
                Debug.Log($"[SecurityCameraVision] {name} - Raycast hit: {lastHitInfo.collider.name} " +
                         $"(IsPlayer: {hitPlayer}, Distance: {lastHitInfo.distance:F2}m)");
            }

            if (!hitPlayer)
            {
                // Blocked by obstacle
                return false;
            }
        }
        else
        {
            if (config.debugStates)
                Debug.Log($"[SecurityCameraVision] {name} - Clear line of sight to player");
        }

        // Clear line of sight
        return true;
    }

    /// <summary>
    /// Get direction to player for camera rotation.
    /// </summary>
    public Vector3 GetDirectionToPlayer()
    {
        if (player == null)
            return transform.forward;

        return (player.position - transform.position).normalized;
    }

    // === DEBUG VISUALIZATION ===

    public void DrawVisionGizmos()
    {
        if (config == null || eyePosition == null)
            return;

        float halfAngle = config.visionAngle * 0.5f;
        float range = config.visionRange;

        // Eye position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eyePosition.position, 0.15f);

        // Forward direction (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(eyePosition.position, transform.forward * range);

        // Vision cone edges (yellow or red if detecting)
        Vector3 forward = transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * forward * range;
        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * forward * range;

        Gizmos.color = lastResult ? Color.red : Color.yellow;
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

        if (player != null)
        {
            Vector3 playerCenter = player.position + Vector3.up * 1f; // Same as raycast
            Vector3 eyePos = eyePosition.position;
            Vector3 dirToPlayer = (playerCenter - eyePos).normalized;
            float distToPlayer = Vector3.Distance(eyePos, playerCenter);

            Vector3 rayStart = eyePos;

            if (lastRaycastHit)
            {
                // Hit something - draw to hit point (RED)
                Vector3 hitPoint = lastHitInfo.point;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayStart, hitPoint);
                Gizmos.DrawWireSphere(hitPoint, 0.1f);

                // Yellow normal
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(hitPoint, hitPoint + lastHitInfo.normal * 0.5f);

                // Dotted line to player (blocked)
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                DrawDottedLine(hitPoint, playerCenter, 0.2f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    hitPoint + Vector3.up * 0.3f,
                    $"BLOCKED: {lastHitInfo.collider.name}\nDist: {lastHitInfo.distance:F2}m",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.red },
                        fontSize = 10
                    }
                );
#endif
            }
            else
            {
                // Clear line of sight (GREEN)
                Gizmos.color = Color.green;
                Gizmos.DrawLine(rayStart, playerCenter);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    playerCenter + Vector3.up * 0.3f,
                    $"VISIBLE\nDist: {distToPlayer:F2}m\nAngle: {lastAngleToPlayer:F1}°",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.green },
                        fontSize = 10
                    }
                );
#endif
            }

            // Raycast start point
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rayStart, 0.08f);

            // Player center marker
            Gizmos.color = lastResult ? Color.green : Color.red;
            Gizmos.DrawWireSphere(playerCenter, 0.2f);
        }

    }

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

    private void OnDrawGizmos()
    {
        if (config != null && config.debugVision)
            DrawVisionGizmos();
    }
}