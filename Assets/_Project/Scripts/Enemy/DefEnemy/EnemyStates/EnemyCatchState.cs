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
        machine.Movement.Stop();
        machine.Animation.SetMoveSpeed(0f);

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyCatch] {machine.gameObject.name} CAUGHT PLAYER!", machine);

        // Deleguj na DeathExecutor
        TriggerDeath();
    }

    private void TriggerDeath()
    {
        if (deathTriggered) return;
        deathTriggered = true;

        // Vypočítej force směr
        Vector3 forceDir = (machine.PlayerTransform.position - machine.transform.position).normalized;
        forceDir.y = 0.2f; // mírně nahoru

        // Notifikuj GameManager s force daty
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerCaught(machine.transform, forceDir, 80f);
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
