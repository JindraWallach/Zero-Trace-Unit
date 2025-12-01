using UnityEngine;

/// <summary>
/// Catch state - Enemy caught player, trigger death sequence.
/// Ultra-minimalist: Just notify GameManager, animation handled by death sequence.
/// </summary>
public class EnemyCatchState : EnemyState
{
    private bool deathTriggered;

    public EnemyCatchState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // Stop enemy movement
        machine.Movement.Stop();

        // Play catch animation (visual feedback only)
        machine.Animation.SetMoveSpeed(0f);
        machine.Animation.PlayCatch();

        deathTriggered = false;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyCatch] {machine.gameObject.name} CAUGHT PLAYER!", machine);

        // Immediately trigger death (GameManager handles timing)
        TriggerDeath();
    }

    private void TriggerDeath()
    {
        if (deathTriggered)
            return;

        deathTriggered = true;

        // Single responsibility: Just notify coordinator
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerCaught();
        }
        else
        {
            Debug.LogError("[EnemyCatch] GameManager.Instance is null!");
        }
    }

    public override void Update()
    {
        // No update needed - death sequence handles everything
    }

    public override void Exit()
    {
        // This state never exits normally (scene reload ends it)
    }

    // No player detection overrides - game is over
    public override void OnPlayerDetected(Vector3 playerPosition) { }
    public override void OnPlayerLost(Vector3 lastKnownPosition) { }
}
