using UnityEngine;

/// <summary>
/// Abstract base class for all enemy AI states.
/// Pattern identical to DoorState for consistency.
/// Each state is responsible for ONE behavior (SRP).
/// CLEAN IMPLEMENTATION - suspicion system only, no legacy.
/// </summary>
public abstract class EnemyState
{
    protected EnemyStateMachine machine;

    public EnemyState(EnemyStateMachine machine)
    {
        this.machine = machine;
    }

    /// <summary>
    /// Called once when entering this state.
    /// Setup animations, speeds, timers, etc.
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// Called once when exiting this state.
    /// Cleanup, stop coroutines, etc.
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// Called every frame while in this state.
    /// Main state logic goes here.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Called when player is detected by vision system.
    /// Override to handle detection in each state.
    /// </summary>
    public virtual void OnPlayerDetected(Vector3 playerPosition) { }

    /// <summary>
    /// Called when player is lost from vision.
    /// Override to handle losing player.
    /// </summary>
    public virtual void OnPlayerLost(Vector3 lastKnownPosition) { }

    /// <summary>
    /// Called when enemy hears a noise.
    /// Override to handle noise detection.
    /// </summary>
    public virtual void OnNoiseHeard(Vector3 noisePosition) { }

    // === HELPER METHODS ===

    /// <summary>
    /// Check if player is currently visible.
    /// </summary>
    protected bool CanSeePlayer()
    {
        return machine.MultiPointVision != null && machine.MultiPointVision.CanSeePlayer;
    }

    /// <summary>
    /// Get number of visible body parts (0-4).
    /// </summary>
    protected int GetVisibleBodyParts()
    {
        return machine.MultiPointVision != null ? machine.MultiPointVision.VisiblePoints : 0;
    }

    /// <summary>
    /// Get current suspicion level (0-100%).
    /// </summary>
    protected float GetSuspicionLevel()
    {
        return machine.Suspicion != null ? machine.Suspicion.Suspicion : 0f;
    }

    /// <summary>
    /// Check if suspicion is in Alert range (30-99%).
    /// </summary>
    protected bool IsInAlertRange()
    {
        return machine.Suspicion != null && machine.Suspicion.IsAlert && !machine.Suspicion.ShouldChase;
    }

    /// <summary>
    /// Check if suspicion reached Chase threshold (100%).
    /// </summary>
    protected bool ShouldChase()
    {
        return machine.Suspicion != null && machine.Suspicion.ShouldChase;
    }

    /// <summary>
    /// Get distance to player.
    /// </summary>
    protected float GetDistanceToPlayer()
    {
        if (machine.PlayerTransform == null)
            return float.MaxValue;

        return Vector3.Distance(machine.transform.position, machine.PlayerTransform.position);
    }

    /// <summary>
    /// Get player position.
    /// </summary>
    protected Vector3 GetPlayerPosition()
    {
        return machine.PlayerTransform != null ? machine.PlayerTransform.position : Vector3.zero;
    }
}