using UnityEngine;

/// <summary>
/// ScriptableObject defining a patrol route with waypoints.
/// Can be shared between multiple enemies or unique per enemy.
/// Create via: Assets > Create > Zero Trace > Patrol Route
/// </summary>
[CreateAssetMenu(fileName = "PatrolRoute", menuName = "Zero Trace/Patrol Route")]
public class PatrolRoute : ScriptableObject
{
    [Header("Waypoints")]
    [Tooltip("List of waypoint positions in world space")]
    public Vector3[] waypoints = new Vector3[0];

    [Tooltip("If true, patrol loops (A→B→C→A). If false, ping-pong (A→B→C→B→A)")]
    public bool loop = true;

    [Tooltip("If true, enemy faces direction of movement. If false, uses custom facing")]
    public bool faceMovementDirection = true;

    [Header("Optional: Custom Facing Directions")]
    [Tooltip("Custom facing direction at each waypoint (if faceMovementDirection = false)")]
    public Vector3[] waypointFacingDirections = new Vector3[0];

    [Header("Debug")]
    [Tooltip("Show waypoints and path in Scene view")]
    public bool debugDraw = true;

    [Tooltip("Color of debug gizmos")]
    public Color gizmoColor = Color.yellow;

    private void OnValidate()
    {
        // Ensure we have at least 2 waypoints
        if (waypoints.Length < 2)
        {
            Debug.LogWarning($"[PatrolRoute] {name} needs at least 2 waypoints for patrol.");
        }

        // Ensure facing directions match waypoints if using custom facing
        if (!faceMovementDirection && waypointFacingDirections.Length != waypoints.Length)
        {
            Debug.LogWarning($"[PatrolRoute] {name}: Custom facing directions count ({waypointFacingDirections.Length}) doesn't match waypoints ({waypoints.Length})");
        }
    }

    /// <summary>
    /// Gets waypoint at index, handling loop/ping-pong logic.
    /// </summary>
    public Vector3 GetWaypoint(int index, out int nextIndex)
    {
        if (waypoints.Length == 0)
        {
            nextIndex = 0;
            return Vector3.zero;
        }

        // Clamp index
        index = Mathf.Clamp(index, 0, waypoints.Length - 1);

        if (loop)
        {
            // Loop: 0→1→2→0
            nextIndex = (index + 1) % waypoints.Length;
        }
        else
        {
            // Ping-pong: 0→1→2→1→0
            if (index >= waypoints.Length - 1)
                nextIndex = waypoints.Length - 2; // Start going back
            else
                nextIndex = index + 1;
        }

        return waypoints[index];
    }

    /// <summary>
    /// Gets facing direction at waypoint (if using custom facing).
    /// </summary>
    public Vector3 GetFacingDirection(int index)
    {
        if (faceMovementDirection || waypointFacingDirections.Length == 0)
            return Vector3.forward;

        index = Mathf.Clamp(index, 0, waypointFacingDirections.Length - 1);
        return waypointFacingDirections[index].normalized;
    }

    /// <summary>
    /// Gets closest waypoint index to given position.
    /// </summary>
    public int GetClosestWaypointIndex(Vector3 position)
    {
        if (waypoints.Length == 0) return 0;

        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(position, waypoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    // Draw gizmos in Scene view
    private void OnDrawGizmos()
    {
        if (!debugDraw || waypoints.Length < 2) return;

        Gizmos.color = gizmoColor;

        // Draw waypoints as spheres
        for (int i = 0; i < waypoints.Length; i++)
        {
            Gizmos.DrawWireSphere(waypoints[i], 0.3f);

            // Draw index label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(waypoints[i] + Vector3.up * 0.5f, $"WP{i}");
#endif
        }

        // Draw path between waypoints
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
        }

        // Draw loop connection if looping
        if (loop && waypoints.Length > 2)
        {
            Gizmos.DrawLine(waypoints[waypoints.Length - 1], waypoints[0]);
        }

        // Draw facing directions if using custom
        if (!faceMovementDirection && waypointFacingDirections.Length == waypoints.Length)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 start = waypoints[i];
                Vector3 end = start + waypointFacingDirections[i].normalized * 1.5f;
                Gizmos.DrawRay(start, end - start);
            }
        }
    }
}