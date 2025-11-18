using UnityEngine;

/// <summary>
/// Catch state - GAME OVER! Enemy caught the player.
/// Alpha version: Simple freeze → disable player → game over after delay.
/// 
/// Timeline:
/// 0.0s - Enemy stops, plays Catch animation
/// 0.1s - Player inputs disabled
/// 1.0s - Game Over triggered (scene reload)
/// 
/// Future v2: Add VFX, camera shake, fade to black
/// </summary>
public class EnemyCatchState : EnemyState
{
    private float catchTimer;
    private bool playerDisabled;

    public EnemyCatchState(EnemyStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // Stop all movement
        machine.Movement.Stop();

        // Face player
        if (machine.PlayerTransform != null)
        {
            machine.Movement.FacePosition(machine.PlayerTransform.position, 20f);
        }

        // Play catch animation
        machine.Animation.SetMoveSpeed(0f);
        machine.Animation.SetAlert(true);
        machine.Animation.PlayCatch();

        catchTimer = 0f;
        playerDisabled = false;

        // Notify state machine
        machine.CatchPlayer();

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyCatch] {machine.gameObject.name} CAUGHT PLAYER!", machine);
    }

    public override void Update()
    {
        catchTimer += Time.deltaTime;

        // Keep facing player during catch
        if (machine.PlayerTransform != null && catchTimer < 0.5f)
        {
            machine.Movement.FacePosition(machine.PlayerTransform.position, 20f);
        }

        // Disable player inputs shortly after catch starts
        if (!playerDisabled && catchTimer >= 0.1f)
        {
            DisablePlayer();
            playerDisabled = true;
        }

        // Trigger game over after delay
        if (catchTimer >= 1.0f)
        {
            TriggerGameOver();
        }
    }

    private void DisablePlayer()
    {
        // Disable player movement via GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerCaught();
        }

        // Optional: Slow motion effect for dramatic impact
        // Time.timeScale = 0.3f; // Slow-mo (remember to reset!)

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyCatch] Player disabled", machine);
    }

    private void TriggerGameOver()
    {
        // Reload scene (simple alpha version)
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.ReloadCurrentScene();
        }
        else
        {
            // Fallback
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyCatch] Game Over triggered - reloading scene", machine);
    }

    public override void Exit()
    {
        // This state should never exit normally (game over ends it)
        // But for safety, reset time scale if we ever do exit
        Time.timeScale = 1f;
    }

    // No player detection overrides needed - game is over
    public override void OnPlayerDetected(Vector3 playerPosition) { }
    public override void OnPlayerLost(Vector3 lastKnownPosition) { }
}