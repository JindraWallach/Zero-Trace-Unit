using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Wrapper for NavMeshAgent movement control.
/// Single Responsibility: Handle all movement logic via NavMesh.
/// States call this to move, stop, set speeds, etc.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementController : MonoBehaviour
{
    private EnemyStateMachine machine;
    private NavMeshAgent agent;
    private Transform target;

    // Current movement state
    private bool isMoving;
    private Vector3 currentDestination;
    private float currentSpeed;

    // Public accessors
    public bool IsMoving => isMoving && agent.velocity.sqrMagnitude > 0.1f;
    public float CurrentSpeed => agent.velocity.magnitude;
    public Vector3 Velocity => agent.velocity;
    public bool HasReachedDestination => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    public Vector3 CurrentDestination => currentDestination;

    public void Initialize(EnemyStateMachine stateMachine)
    {
        machine = stateMachine;
        agent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent
        agent.speed = machine.Config.patrolSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.5f;
        agent.autoBraking = true;

        if (machine.Config.debugMovement)
            Debug.Log($"[EnemyMovement] {gameObject.name} initialized", this);
    }

    /// <summary>
    /// Move to a specific world position.
    /// </summary>
    public void MoveToPosition(Vector3 position, float speed)
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"[EnemyMovement] {gameObject.name} not on NavMesh!", this);
            return;
        }

        currentDestination = position;
        currentSpeed = speed;
        agent.speed = speed;
        agent.isStopped = false;
        agent.SetDestination(position);
        isMoving = true;

        if (machine.Config.debugMovement)
            Debug.Log($"[EnemyMovement] {gameObject.name} moving to {position} at speed {speed}", this);
    }

    /// <summary>
    /// Chase a moving target (player).
    /// Call every frame in Chase state.
    /// </summary>
    public void ChaseTarget(Transform target, float speed)
    {
        if (target == null) return;

        this.target = target;
        MoveToPosition(target.position, speed);
    }

    /// <summary>
    /// Stop all movement immediately.
    /// </summary>
    public void Stop()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        isMoving = false;
        target = null;

        if (machine.Config.debugMovement)
            Debug.Log($"[EnemyMovement] {gameObject.name} stopped", this);
    }

    /// <summary>
    /// Check if current path is blocked (for door detection).
    /// </summary>
    public bool IsPathBlocked()
    {
        if (!agent.isOnNavMesh)
            return true;

        // Check if path is partial (blocked by obstacle)
        return agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial;
    }

    /// <summary>
    /// Resume movement after stopping.
    /// </summary>
    public void Resume()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
        isMoving = true;
    }

    /// <summary>
    /// Set movement speed (useful for transitions).
    /// </summary>
    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
        agent.speed = speed;
    }

    /// <summary>
    /// Rotate to face a direction smoothly.
    /// </summary>
    public void FaceDirection(Vector3 direction, float rotationSpeed = 5f)
    {
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Rotate to face a position smoothly.
    /// </summary>
    public void FacePosition(Vector3 position, float rotationSpeed = 5f)
    {
        Vector3 direction = (position - transform.position).normalized;
        direction.y = 0; // Keep rotation on horizontal plane
        FaceDirection(direction, rotationSpeed);
    }

    /// <summary>
    /// Check if destination is reachable via NavMesh.
    /// </summary>
    public bool IsDestinationReachable(Vector3 destination)
    {
        NavMeshPath path = new NavMeshPath();
        return agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Get distance to current destination.
    /// </summary>
    public float GetRemainingDistance()
    {
        if (!agent.isOnNavMesh || agent.pathPending)
            return float.MaxValue;

        return agent.remainingDistance;
    }

    /// <summary>
    /// Check if position is within range.
    /// </summary>
    public bool IsWithinRange(Vector3 position, float range)
    {
        return Vector3.Distance(transform.position, position) <= range;
    }

    /// <summary>
    /// Get random point within radius (for searching).
    /// </summary>
    public Vector3 GetRandomPointInRadius(Vector3 center, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return center; // Fallback to center if no valid point found
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (machine == null || !machine.Config.debugMovement) return;

        // Draw path
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
                Gizmos.DrawWireSphere(corners[i], 0.2f);
            }
        }

        // Draw destination
        if (isMoving)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentDestination, 0.5f);
            Gizmos.DrawLine(transform.position, currentDestination);
        }

        // Draw stopping distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, agent != null ? agent.stoppingDistance : 0.5f);
    }
}