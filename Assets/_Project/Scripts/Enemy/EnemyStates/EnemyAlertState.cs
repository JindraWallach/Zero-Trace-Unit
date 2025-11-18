using UnityEngine;

/// <summary>
/// Alert state - enemy heard/saw something suspicious.
/// Briefly investigates, then either:
/// - Transitions to Chase if player confirmed
/// - Transitions to Search if player not visible
/// - Returns to Patrol/Idle if nothing found
/// </summary>
public class EnemyAlertState : EnemyState
{
    private Vector3 alertPosition;
    private float alertTimer;
    private bool isInvestigating;

    public EnemyAlertState(EnemyStateMachine machine, Vector3 suspiciousPosition) : base(machine)
    {
        alertPosition = suspiciousPosition;
    }

    public override void Enter()
    {
        // Stop patrol, face alert position
        machine.Movement.Stop();
        machine.Movement.FacePosition(alertPosition);

        // Set alert animation (combat-ready stance)
        machine.Animation.SetAlert(true);
        machine.Animation.SetMoveSpeed(0f);

        alertTimer = 0f;
        isInvestigating = false;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyAlert] {machine.gameObject.name} alerted at position {alertPosition}", machine);
    }

    public override void Update()
    {
        alertTimer += Time.deltaTime;

        // Keep facing alert position
        if (!isInvestigating)
        {
            machine.Movement.FacePosition(alertPosition);
        }

        // Check if we can see player now
        if (CanSeePlayer())
        {
            // Player confirmed - chase!
            machine.SetState(new EnemyChaseState(machine));
            return;
        }

        // After alert duration, decide next action
        if (alertTimer >= machine.Config.alertToSearchDelay)
        {
            if (machine.HasSeenPlayer)
            {
                // We've seen player before - go search last known position
                machine.SetState(new EnemySearchState(machine, machine.LastKnownPlayerPosition));
            }
            else
            {
                // False alarm - return to patrol/idle
                ReturnToNormalBehavior();
            }
        }
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Player spotted during alert - chase immediately
        machine.SetState(new EnemyChaseState(machine));
    }

    public override void OnPlayerLost(Vector3 lastKnownPosition)
    {
        // Update alert position to last known
        alertPosition = lastKnownPosition;
    }

    private void ReturnToNormalBehavior()
    {
        // Return to patrol or idle based on route availability
        if (machine.PatrolRoute != null && machine.PatrolRoute.waypoints.Length >= 2)
        {
            machine.SetState(new EnemyPatrolState(machine));
        }
        else
        {
            machine.SetState(new EnemyIdleState(machine));
        }
    }

    public override void Exit()
    {
        // Clear alert animation
        machine.Animation.SetAlert(false);
    }
}