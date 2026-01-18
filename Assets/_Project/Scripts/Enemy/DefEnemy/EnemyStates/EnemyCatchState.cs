using UnityEngine;

public class EnemyCatchState : EnemyState
{
    private bool deathTriggered;

    public EnemyCatchState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Movement.Stop();
        machine.Animation.SetMoveSpeed(0f);
        machine.Animation.PlayCatch();

        deathTriggered = false;

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyCatch] {machine.gameObject.name} CAUGHT PLAYER!", machine);

        // Trigger s delayem
        machine.StartCoroutine(DelayedDeath());
    }

    private System.Collections.IEnumerator DelayedDeath()
    {
        // Čas na taser trail/FX
        yield return new WaitForSeconds(machine.Config.taserHitDelay);

        TriggerDeath();
    }

    private void TriggerDeath()
    {
        if (deathTriggered) return;
        deathTriggered = true;

        // Vypočítej force směr
        Vector3 forceDir = (machine.PlayerTransform.position - machine.transform.position).normalized;
        forceDir.y = machine.Config.catchForceVertical;

        // Pošli do GameManageru s force z configu
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerCaught(
                machine.TaserSpawnPoint,
                forceDir,
                machine.Config.catchForceMagnitude
            );
        }
        else
        {
            Debug.LogError("[EnemyCatch] GameManager.Instance is null!");
        }
    }

    public override void Update() { }
    public override void Exit() { }
    public override void OnPlayerDetected(Vector3 playerPosition) { }
    public override void OnPlayerLost(Vector3 lastKnownPosition) { }
}