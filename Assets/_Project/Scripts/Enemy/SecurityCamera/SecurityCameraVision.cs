using UnityEngine;

/// <summary>
/// Simplified FOV detection for security cameras.
/// Timer-based checks for performance (no batch manager needed).
/// </summary>
public class SecurityCameraVision : MonoBehaviour
{
    private SecurityCameraConfig config;
    private Transform player;
    private Transform eyePosition;

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

        // Find or create eye position
        eyePosition = transform.Find("EyePosition");
        if (eyePosition == null)
        {
            GameObject eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = Vector3.zero;
            eyePosition = eyeObj.transform;
        }

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

        Vector3 playerPosition = player.position;
        Vector3 directionToPlayer = (playerPosition - eyePosition.position).normalized;
        float distanceToPlayer = Vector3.Distance(eyePosition.position, playerPosition);

        lastDirectionToPlayer = directionToPlayer;

        // Check 1: Range
        if (distanceToPlayer > config.visionRange)
            return false;

        // Check 2: Angle (FOV cone)
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        lastAngleToPlayer = angleToPlayer;

        if (angleToPlayer > config.visionAngle * 0.5f)
            return false;

        // Check 3: Raycast (obstacles)
        Vector3 rayStart = eyePosition.position + directionToPlayer * 0.1f;
        float rayDistance = distanceToPlayer - 0.1f;

        lastRaycastHit = Physics.Raycast(rayStart, directionToPlayer, out lastHitInfo, rayDistance, config.visionObstacleMask);

        if (lastRaycastHit)
        {
            // Check if hit player or obstacle
            bool hitPlayer = lastHitInfo.collider.CompareTag("Player") || lastHitInfo.collider.transform == player;
            return hitPlayer;
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

        // Direction to player
        if (player != null)
        {
            Vector3 playerPos = player.position;
            Gizmos.color = lastResult ? Color.green : Color.red;
            Gizmos.DrawLine(eyePosition.position, playerPos);
            Gizmos.DrawWireSphere(playerPos, 0.3f);
        }
    }

    private void OnDrawGizmos()
    {
        if (config != null && config.debugVision)
            DrawVisionGizmos();
    }
}