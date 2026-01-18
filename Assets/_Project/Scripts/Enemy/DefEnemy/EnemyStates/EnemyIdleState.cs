using UnityEngine;

/// <summary>
/// Idle state - enemy stands still and looks around.
/// Used when no patrol route assigned or at waypoints.
/// Transitions to Patrol/Alert/Chase based on patrol route or player detection.
/// UPDATED: Uses suspicion system for gradual detection.
/// </summary>
public class EnemyIdleState : EnemyState
{
    private float idleTimer;
    private float nextLookAroundTime;
    private const float LOOK_AROUND_INTERVAL = 3f;

    public EnemyIdleState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // Stop movement
        machine.Movement.Stop();

        // Set relaxed animation
        machine.Animation.SetAlert(false);
        machine.Animation.SetMoveSpeed(0f);

        idleTimer = 0f;
        nextLookAroundTime = LOOK_AROUND_INTERVAL;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyIdle] {machine.gameObject.name} entered Idle state", machine);
    }

    public override void Update()
    {
        idleTimer += Time.deltaTime;

        // Periodically rotate to look around (optional flavor)
        if (idleTimer >= nextLookAroundTime)
        {
            LookAround();
            nextLookAroundTime = idleTimer + LOOK_AROUND_INTERVAL;
        }

        // Check if we have a patrol route to follow
        if (machine.PatrolRoute != null && machine.PatrolRoute.WaypointCount >= 2)
        {
            // Transition to patrol after brief idle
            if (idleTimer >= machine.Config.waypointWaitTime)
            {
                machine.SetState(new EnemyPatrolState(machine));
            }
        }
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Player spotted while idle - go to alert
        // (Suspicion system will handle transition to chase at 100%)
        machine.SetState(new EnemyAlertState(machine, playerPosition));
    }

    public override void OnNoiseHeard(Vector3 noisePosition)
    {
        // Heard noise while idle → investigate (suspicious state)
        var suspiciousState = new EnemySuspiciousState(machine, noisePosition);
        machine.SetState(suspiciousState);

        if (machine.Config.debugStates)
        {
            Debug.Log($"[EnemyIdle] {machine.gameObject.name} heard noise at {noisePosition}, " +
                     $"entering suspicious state", machine);
        }
    }

    private void LookAround()
    {
        // Slowly rotate to random direction (adds life to idle)
        float randomAngle = Random.Range(-45f, 45f);
        Vector3 newDirection = Quaternion.Euler(0, randomAngle, 0) * machine.transform.forward;
        machine.Movement.FaceDirection(newDirection, 2f);
    }
}