using UnityEngine;

/// <summary>
/// Transform-based patrol route using child GameObjects as waypoints.
/// Optimized with caching for performance (SRP: waypoint management only).
/// </summary>
public class PatrolRoute : MonoBehaviour
{
    [Header("Patrol Behavior")]
    [Tooltip("If true, patrol loops (A→B→C→A). If false, ping-pong (A→B→C→B→A)")]
    public bool loop = true;

    [Tooltip("If true, enemy faces direction of movement. If false, uses waypoint rotation")]
    public bool faceMovementDirection = true;

    [Header("Debug")]
    [Tooltip("Show waypoints and path in Scene view")]
    public bool debugDraw = true;

    [Tooltip("Color of debug gizmos")]
    public Color gizmoColor = Color.yellow;

    // Cached data (performance optimization)
    private Transform[] waypointTransforms;
    private int waypointCount;

    private void Awake()
    {
        CacheWaypoints();
    }

    private void OnValidate()
    {
        CacheWaypoints();
    }

    /// <summary>
    /// Cache child transforms as waypoints (called once on start + in editor).
    /// </summary>
    private void CacheWaypoints()
    {
        waypointCount = transform.childCount;
        waypointTransforms = new Transform[waypointCount];

        for (int i = 0; i < waypointCount; i++)
        {
            waypointTransforms[i] = transform.GetChild(i);
        }

        if (waypointCount < 2)
        {
            Debug.LogWarning($"[PatrolRoute] {name} needs at least 2 child waypoints for patrol.", this);
        }
    }

    /// <summary>
    /// Get waypoint count.
    /// </summary>
    public int WaypointCount => waypointCount;

    /// <summary>
    /// Get waypoint position at index (live from Transform).
    /// </summary>
    public Vector3 GetWaypointPosition(int index)
    {
        if (waypointTransforms == null || index < 0 || index >= waypointCount)
            return Vector3.zero;

        return waypointTransforms[index].position;
    }

    /// <summary>
    /// Get waypoint at index, handling loop/ping-pong logic.
    /// </summary>
    public Vector3 GetWaypoint(int index, out int nextIndex)
    {
        if (waypointCount == 0)
        {
            nextIndex = 0;
            return Vector3.zero;
        }

        // Clamp index
        index = Mathf.Clamp(index, 0, waypointCount - 1);

        if (loop)
        {
            // Loop: 0→1→2→0
            nextIndex = (index + 1) % waypointCount;
        }
        else
        {
            // Ping-pong: 0→1→2→1→0
            if (index >= waypointCount - 1)
                nextIndex = waypointCount - 2; // Start going back
            else
                nextIndex = index + 1;
        }

        return waypointTransforms[index].position;
    }

    /// <summary>
    /// Gets facing direction at waypoint (if using custom facing).
    /// </summary>
    public Vector3 GetFacingDirection(int index)
    {
        if (faceMovementDirection || waypointTransforms == null || waypointCount == 0)
            return Vector3.forward;

        index = Mathf.Clamp(index, 0, waypointCount - 1);
        return waypointTransforms[index].forward;
    }

    /// <summary>
    /// Gets closest waypoint index to given position.
    /// </summary>
    public int GetClosestWaypointIndex(Vector3 position)
    {
        if (waypointCount == 0) return 0;

        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < waypointCount; i++)
        {
            float distance = Vector3.Distance(position, waypointTransforms[i].position);
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
        if (!debugDraw) return;

        // Update cache if in editor
        if (!Application.isPlaying)
            CacheWaypoints();

        if (waypointCount < 2) return;

        Gizmos.color = gizmoColor;

        // Draw waypoints as spheres
        for (int i = 0; i < waypointCount; i++)
        {
            if (waypointTransforms[i] == null) continue;

            Vector3 pos = waypointTransforms[i].position;
            Gizmos.DrawWireSphere(pos, 0.3f);

            // Draw index label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"WP{i}");
#endif
        }

        // Draw path between waypoints
        for (int i = 0; i < waypointCount - 1; i++)
        {
            if (waypointTransforms[i] == null || waypointTransforms[i + 1] == null)
                continue;

            Gizmos.DrawLine(waypointTransforms[i].position, waypointTransforms[i + 1].position);
        }

        // Draw loop connection if looping
        if (loop && waypointCount > 2)
        {
            if (waypointTransforms[waypointCount - 1] != null && waypointTransforms[0] != null)
            {
                Gizmos.DrawLine(waypointTransforms[waypointCount - 1].position, waypointTransforms[0].position);
            }
        }

        // Draw facing directions if using custom
        if (!faceMovementDirection)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypointCount; i++)
            {
                if (waypointTransforms[i] == null) continue;

                Vector3 start = waypointTransforms[i].position;
                Vector3 end = start + waypointTransforms[i].forward * 1.5f;
                Gizmos.DrawRay(start, end - start);
            }
        }
    }
}