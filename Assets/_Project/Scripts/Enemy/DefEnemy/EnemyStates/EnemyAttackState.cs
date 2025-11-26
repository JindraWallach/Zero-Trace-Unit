using UnityEngine;

/// <summary>
/// Attack state - enemy is in attack range and performs attack.
/// Plays attack animation, then returns to Chase or Catch.
/// Optional: Can damage player here (for non-instant-kill gameplay).
/// </summary>
public class EnemyAttackState : EnemyState
{
    private float attackTimer;
    private bool hasAttacked;

    public EnemyAttackState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // Stop movement and face player
        machine.Movement.Stop();

        if (machine.PlayerTransform != null)
        {
            machine.Movement.FacePosition(machine.PlayerTransform.position, 10f);
        }

        // Set alert animation
        machine.Animation.SetAlert(true);
        machine.Animation.SetMoveSpeed(0f);

        // Play attack animation
        machine.Animation.PlayAttack();

        attackTimer = 0f;
        hasAttacked = false;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyAttack] {machine.gameObject.name} attacking player!", machine);
    }

    public override void Update()
    {
        attackTimer += Time.deltaTime;

        // Keep facing player during attack
        if (machine.PlayerTransform != null)
        {
            machine.Movement.FacePosition(machine.PlayerTransform.position, 10f);
        }

        // Check if player is still in range
        float distanceToPlayer = GetDistanceToPlayer();

        // If player is in catch range - instant game over
        if (distanceToPlayer <= machine.Config.catchRange)
        {
            machine.SetState(new EnemyCatchState(machine));
            return;
        }

        // Attack hit frame (middle of animation, adjust timing as needed)
        if (!hasAttacked && attackTimer >= 0.3f)
        {
            PerformAttackHit();
            hasAttacked = true;
        }

        // After attack cooldown, return to chase
        if (attackTimer >= machine.Config.attackCooldown)
        {
            // Check if player still in range
            if (distanceToPlayer <= machine.Config.attackRange && CanSeePlayer())
            {
                // Attack again
                machine.SetState(new EnemyAttackState(machine));
            }
            else
            {
                // Return to chase
                machine.SetState(new EnemyChaseState(machine));
            }
        }
    }

    public override void OnPlayerLost(Vector3 lastKnownPosition)
    {
        // Player escaped during attack - search
        machine.SetState(new EnemySearchState(machine, lastKnownPosition));
    }

    private void PerformAttackHit()
    {
        // Optional: Deal damage to player here
        // For now, attacks are just theatrical until catch range reached

        float distanceToPlayer = GetDistanceToPlayer();

        if (distanceToPlayer <= machine.Config.attackRange)
        {
            // TODO: If implementing health system:
            // PlayerHealth.Instance?.TakeDamage(10);

            if (machine.Config.debugStates)
                Debug.Log($"[EnemyAttack] {machine.gameObject.name} attack connected!", machine);
        }
    }

    public override void Exit()
    {
        // Resume movement capability
        machine.Movement.Resume();
    }
}