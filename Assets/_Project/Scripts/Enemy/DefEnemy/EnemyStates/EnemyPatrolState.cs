using UnityEngine;

/// <summary>
/// Patrol state - enemy walks between waypoints.
/// Deterministic route following (no randomness).
/// Waits at each waypoint before moving to next.
/// Transitions to Alert/Chase when player detected.
/// UPDATED: Uses suspicion system for gradual detection.
/// </summary>
public class EnemyPatrolState : EnemyState
{
    private int currentWaypointIndex;
    private bool isWaiting;
    private float waitTimer;
    private bool isPingPongReversing;

    public EnemyPatrolState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // Set patrol speed and relaxed animation
        machine.Animation.SetAlert(false);

        // Find closest waypoint to start from
        if (machine.PatrolRoute != null)
        {
            currentWaypointIndex = machine.PatrolRoute.GetClosestWaypointIndex(machine.transform.position);
        }

        isWaiting = false;
        waitTimer = 0f;
        isPingPongReversing = false;

        // Start moving to first waypoint
        MoveToCurrentWaypoint();

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyPatrol] {machine.gameObject.name} entered Patrol state, starting at waypoint {currentWaypointIndex}", machine);
    }

    public override void Update()
    {
        if (isWaiting)
        {
            // Wait at waypoint
            waitTimer += Time.deltaTime;

            if (waitTimer >= machine.Config.waypointWaitTime)
            {
                // Wait complete - move to next waypoint
                AdvanceToNextWaypoint();
                MoveToCurrentWaypoint();
                isWaiting = false;
            }
        }
        else
        {
            // Moving to waypoint
            if (machine.Movement.HasReachedDestination)
            {
                // Reached waypoint - start waiting
                StartWaiting();
            }
        }
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Player spotted during patrol
        // Suspicion system will handle transition (Alert at 30%, Chase at 100%)
        if (IsInAlertRange())
        {
            machine.SetState(new EnemyAlertState(machine, playerPosition));
        }
    }

    public override void OnNoiseHeard(Vector3 noisePosition)
    {
        // Heard noise during patrol - investigate
        machine.SetState(new EnemyAlertState(machine, noisePosition));

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyPatrol] {machine.gameObject.name} heard noise, investigating {noisePosition}", machine);
    }

    private void MoveToCurrentWaypoint()
    {
        if (machine.PatrolRoute == null) return;

        Vector3 waypoint = machine.PatrolRoute.GetWaypointPosition(currentWaypointIndex);
        machine.Movement.MoveToPosition(waypoint, machine.Config.patrolSpeed);

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyPatrol] {machine.gameObject.name} moving to waypoint {currentWaypointIndex}: {waypoint}", machine);
    }

    private void AdvanceToNextWaypoint()
    {
        if (machine.PatrolRoute == null) return;

        int waypointCount = machine.PatrolRoute.WaypointCount;

        if (machine.PatrolRoute.loop)
        {
            // Loop mode: 0 → 1 → 2 → 0
            currentWaypointIndex = (currentWaypointIndex + 1) % waypointCount;
        }
        else
        {
            // Ping-pong mode: 0 → 1 → 2 → 1 → 0
            if (isPingPongReversing)
            {
                currentWaypointIndex--;
                if (currentWaypointIndex <= 0)
                {
                    currentWaypointIndex = 0;
                    isPingPongReversing = false;
                }
            }
            else
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypointCount - 1)
                {
                    currentWaypointIndex = waypointCount - 1;
                    isPingPongReversing = true;
                }
            }
        }
    }

    private void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;

        // Face custom direction if specified
        if (!machine.PatrolRoute.faceMovementDirection)
        {
            Vector3 facingDir = machine.PatrolRoute.GetFacingDirection(currentWaypointIndex);
            if (facingDir != Vector3.zero)
            {
                machine.Movement.FaceDirection(facingDir, 3f);
            }
        }

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyPatrol] {machine.gameObject.name} waiting at waypoint {currentWaypointIndex}", machine);
    }

    public override void Exit()
    {
        // Stop movement when leaving patrol
        machine.Movement.Stop();
    }
}