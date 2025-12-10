using UnityEngine;

public class EnemyChaseState : EnemyState
{
    private float chaseTimer;
    private float lastSeenTimer;
    private const float LOSE_PLAYER_DELAY = 2f;

    // Path blocking detection
    private float blockedTimer;
    private float pathCheckTimer;
    private const float BLOCKED_TIMEOUT = 5f;
    private const float PATH_CHECK_INTERVAL = 0.5f;

    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Animation.SetAlert(true);
        chaseTimer = 0f;
        lastSeenTimer = 0f;
        blockedTimer = 0f;
        pathCheckTimer = 0f;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyChase] {machine.gameObject.name} started chasing player!", machine);
    }

    public override void Update()
    {
        chaseTimer += Time.deltaTime;
        pathCheckTimer += Time.deltaTime;

        // === PATH BLOCKING CHECK (optimized - every 0.5s) ===
        if (pathCheckTimer >= PATH_CHECK_INTERVAL)
        {
            pathCheckTimer = 0f;

            if (machine.Movement.IsPathBlocked())
            {
                blockedTimer += PATH_CHECK_INTERVAL;

                // Path blocked too long - give up and patrol
                if (blockedTimer >= BLOCKED_TIMEOUT)
                {
                    if (machine.Config.debugStates)
                    {
                        Debug.Log($"[EnemyChase] {machine.gameObject.name} path blocked for {BLOCKED_TIMEOUT}s, " +
                                 $"returning to patrol", machine);
                    }

                    machine.SetState(new EnemyPatrolState(machine));
                    return;
                }
            }
            else
            {
                blockedTimer = 0f;
            }
        }

        // === VISION CHECK ===
        bool canSeePlayer = CanSeePlayer();

        // Player NOT visible - handle lost sight
        if (!canSeePlayer)
        {
            lastSeenTimer += Time.deltaTime;

            // Move to last known position
            if (machine.HasSeenPlayer)
            {
                machine.Movement.MoveToPosition(machine.LastKnownPlayerPosition, machine.Config.chaseSpeed);
            }

            // Lost sight too long - transition to search
            if (lastSeenTimer >= LOSE_PLAYER_DELAY)
            {
                machine.SetState(new EnemySearchState(machine, machine.LastKnownPlayerPosition));
            }

            return;
        }

        // === PLAYER VISIBLE - chase logic ===
        lastSeenTimer = 0f;

        // Early exit: no player transform
        if (machine.PlayerTransform == null)
            return;

        Vector3 playerPos = machine.PlayerTransform.position;
        float distanceToPlayer = GetDistanceToPlayer();

        // Check catch range (highest priority)
        if (distanceToPlayer <= machine.Config.catchRange)
        {
            machine.SetState(new EnemyCatchState(machine));
            return;
        }

        // Check attack range
        if (distanceToPlayer <= machine.Config.attackRange)
        {
            machine.SetState(new EnemyAttackState(machine));
            return;
        }

        // Chase player
        machine.Movement.ChaseTarget(machine.PlayerTransform, machine.Config.chaseSpeed);
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Already chasing, reset timer
        lastSeenTimer = 0f;
    }

    public override void OnPlayerLost(Vector3 lastKnownPosition)
    {
        // Start counting time since lost
        lastSeenTimer = 0f;
    }

    public override void Exit()
    {
        machine.Movement.Stop();
    }
}