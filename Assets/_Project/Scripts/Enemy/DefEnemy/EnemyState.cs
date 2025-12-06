using UnityEngine;

/// <summary>
/// Abstract base class for all enemy AI states.
/// Pattern identical to DoorState for consistency.
/// Each state is responsible for ONE behavior (SRP).
/// UPDATED: Compatible with both suspicion system and legacy vision.
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

    // === HELPER METHODS (Compatible with both systems) ===

    /// <summary>
    /// Check if player is in vision range.
    /// Works with both suspicion system and legacy vision.
    /// </summary>
    protected bool CanSeePlayer()
    {
        // NEW SYSTEM: Check suspicion system first
        if (machine.UsingSuspicionSystem)
        {
            if (machine.Suspicion != null)
            {
                return machine.Suspicion.IsPlayerVisible;
            }

            if (machine.MultiPointVision != null)
            {
                return machine.MultiPointVision.CanSeePlayer;
            }
        }

        // LEGACY SYSTEM: Fallback to old vision detector
        var legacyVision = machine.GetComponent<EnemyVisionDetector>();
        if (legacyVision != null)
        {
            return legacyVision.CanSeePlayer(out _);
        }

        // No vision system available
        return false;
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

    /// <summary>
    /// Get current suspicion level (0-100%).
    /// Returns 0 if suspicion system not enabled.
    /// </summary>
    protected float GetSuspicionLevel()
    {
        if (machine.Suspicion != null)
            return machine.Suspicion.Suspicion;

        return 0f;
    }

    /// <summary>
    /// Check if suspicion is in Alert range (30-99%).
    /// </summary>
    protected bool IsInAlertRange()
    {
        if (machine.Suspicion != null)
            return machine.Suspicion.IsAlert && !machine.Suspicion.ShouldChase;

        return false;
    }

    /// <summary>
    /// Check if suspicion reached Chase threshold (100%).
    /// </summary>
    protected bool ShouldChase()
    {
        if (machine.Suspicion != null)
            return machine.Suspicion.ShouldChase;

        // Legacy: instant chase when seeing player
        return CanSeePlayer();
    }
}