using UnityEngine;

/// <summary>
/// Chase state - enemy actively pursues player.
/// Continuously follows player while in vision.
/// Checks for catch range to trigger game over.
/// Transitions to Search if player lost.
/// </summary>
public class EnemyChaseState : EnemyState
{
    private float chaseTimer;
    private float lastSeenTimer;
    private const float LOSE_PLAYER_DELAY = 2f; // Grace period before giving up

    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // Set chase speed and alert animation
        machine.Animation.SetAlert(true);

        chaseTimer = 0f;
        lastSeenTimer = 0f;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyChase] {machine.gameObject.name} started chasing player!", machine);
    }

    public override void Update()
    {
        chaseTimer += Time.deltaTime;

        // Check if player is in vision
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            // Player visible - chase actively
            lastSeenTimer = 0f;

            if (machine.PlayerTransform != null)
            {
                Vector3 playerPos = machine.PlayerTransform.position;
                float distanceToPlayer = GetDistanceToPlayer();

                // Check for CATCH range (game over)
                if (distanceToPlayer <= machine.Config.catchRange)
                {
                    machine.SetState(new EnemyCatchState(machine));
                    return;
                }

                // Check for attack range
                if (distanceToPlayer <= machine.Config.attackRange)
                {
                    machine.SetState(new EnemyAttackState(machine));
                    return;
                }

                // Chase player
                machine.Movement.ChaseTarget(machine.PlayerTransform, machine.Config.chaseSpeed);
            }
        }
        else
        {
            // Player not visible - continue to last known position
            lastSeenTimer += Time.deltaTime;

            // Move to last known position
            if (machine.HasSeenPlayer)
            {
                machine.Movement.MoveToPosition(machine.LastKnownPlayerPosition, machine.Config.chaseSpeed);
            }

            // If lost sight for too long, transition to search
            if (lastSeenTimer >= LOSE_PLAYER_DELAY)
            {
                machine.SetState(new EnemySearchState(machine, machine.LastKnownPlayerPosition));
            }
        }
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Already chasing, just reset timer
        lastSeenTimer = 0f;
    }

    public override void OnPlayerLost(Vector3 lastKnownPosition)
    {
        // Start counting time since lost
        lastSeenTimer = 0f;
    }

    public override void Exit()
    {
        // Stop chasing
        machine.Movement.Stop();
    }
}