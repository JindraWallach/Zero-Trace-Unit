using UnityEngine;

/// <summary>
/// Abstract base class for all enemy AI states.
/// Pattern identical to DoorState for consistency.
/// Each state is responsible for ONE behavior (SRP).
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
    /// Called when enemy hears a noise (v3.0).
    /// Override to handle noise detection.
    /// </summary>
    public virtual void OnNoiseHeard(Vector3 noisePosition) { }

    // Helper: Check if player is in vision range
    protected bool CanSeePlayer()
    {
        return machine.Vision.CanSeePlayer(out _);
    }

    // Helper: Get distance to player
    protected float GetDistanceToPlayer()
    {
        if (machine.PlayerTransform == null) return float.MaxValue;
        return Vector3.Distance(machine.transform.position, machine.PlayerTransform.position);
    }

    // Helper: Get player position
    protected Vector3 GetPlayerPosition()
    {
        return machine.PlayerTransform != null ? machine.PlayerTransform.position : Vector3.zero;
    }
}