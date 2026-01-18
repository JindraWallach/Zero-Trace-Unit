using System;
using UnityEngine;

/// <summary>
/// Suspicious state - enemy heard noise but hasn't seen player.
/// Investigates sound location, uses EnemySuspicionSystem for gradual detection.
/// Replaces EnemyAlertState for noise-based investigation.
/// </summary>
public class EnemySuspiciousState : EnemyState
{
    private Vector3 investigatePosition;
    private float investigateTimer;

    private EnemySuspicionSystem suspicionSystem;

    // Events for external systems
    public event Action OnAlertTriggered;
    public event Action OnChaseTriggered;
    public event Action OnSuspicionCleared;

    public EnemySuspicionSystem SuspicionRef => suspicionSystem;

    public EnemySuspiciousState(EnemyStateMachine machine, Vector3 position) : base(machine)
    {
        investigatePosition = position;
    }

    public void Initialize(EnemySuspicionSystem system)
    {
        suspicionSystem = system;
    }

    public override void Enter()
    {
        // Get suspicion system from machine
        if (suspicionSystem == null)
        {
            suspicionSystem = machine.Suspicion;
        }

        if (suspicionSystem == null)
        {
            Debug.LogError($"[EnemySuspicious] {machine.gameObject.name} missing EnemySuspicionSystem!", machine);
            machine.SetState(new EnemyPatrolState(machine));
            return;
        }

        machine.Animation.SetAlert(true);
        machine.Animation.SetMoveSpeed(0f);

        // Set initial suspicion from noise (from config)
        float initialSuspicion = machine.Config.suspicionConfig.noiseInitialSuspicion;
        if (suspicionSystem.Suspicion < initialSuspicion)
        {
            suspicionSystem.AddSuspicion(initialSuspicion - suspicionSystem.Suspicion);
        }

        investigateTimer = 0f;

        // Face investigate position
        machine.Movement.FacePosition(investigatePosition, 5f);

        if (machine.Config.debugStates)
        {
            Debug.Log($"[EnemySuspicious] {machine.gameObject.name} investigating noise at {investigatePosition} " +
                     $"(suspicion: {suspicionSystem.Suspicion:F0}%)", machine);
        }
    }

    public override void Update()
    {
        investigateTimer += Time.deltaTime;

        // Vision check handled by EnemyMultiPointVision → updates suspicionSystem automatically
        // Check if should chase (suspicion >= 100%)
        if (suspicionSystem.ShouldChase)
        {
            machine.SetState(new EnemyChaseState(machine));
            OnChaseTriggered?.Invoke();
            return;
        }

        // Face position initially, then move (duration from config)
        if (investigateTimer < machine.Config.suspicionConfig.suspiciousFaceDuration)
        {
            machine.Movement.FacePosition(investigatePosition, 5f);
        }
        else if (!machine.Movement.HasReachedDestination)
        {
            // Move to investigate
            machine.Movement.MoveToPosition(investigatePosition, machine.Config.searchSpeed);
        }

        // Investigation complete (duration from config)
        if ((machine.Movement.HasReachedDestination && investigateTimer > machine.Config.suspicionConfig.suspiciousFaceDuration) ||
            investigateTimer >= machine.Config.suspicionConfig.suspiciousInvestigationDuration)
        {
            // Check suspicion level
            if (suspicionSystem.Suspicion <= 10f)
            {
                // Low suspicion → return to normal
                ClearSuspicion();
                ReturnToNormalBehavior();
            }
            else if (suspicionSystem.IsAlert)
            {
                // High suspicion but no visual → search area
                machine.SetState(new EnemySearchState(machine, investigatePosition));
            }
            else
            {
                // Medium suspicion → wait a bit more
                if (investigateTimer >= machine.Config.suspicionConfig.suspiciousInvestigationDuration * 1.5f)
                {
                    ReturnToNormalBehavior();
                }
            }
        }
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Visual detection during investigation → chase immediately
        machine.SetState(new EnemyChaseState(machine));
        OnChaseTriggered?.Invoke();
    }

    public override void OnNoiseHeard(Vector3 noisePosition)
    {
        // Heard another noise while investigating
        float distanceToNewNoise = Vector3.Distance(machine.transform.position, noisePosition);
        float distanceToCurrentTarget = Vector3.Distance(machine.transform.position, investigatePosition);

        // If new noise is closer, redirect
        if (distanceToNewNoise < distanceToCurrentTarget)
        {
            investigatePosition = noisePosition;
            investigateTimer = 0f;

            // Add suspicion (from config)
            suspicionSystem.AddSuspicion(machine.Config.suspicionConfig.noiseAdditionalSuspicion);

            if (machine.Config.debugStates)
            {
                Debug.Log($"[EnemySuspicious] {machine.gameObject.name} heard closer noise, redirecting " +
                         $"(suspicion: {suspicionSystem.Suspicion:F0}%)", machine);
            }

            // Check alert threshold (80%)
            if (suspicionSystem.Suspicion >= 80f)
            {
                OnAlertTriggered?.Invoke();
            }
        }
    }

    public void ClearSuspicion()
    {
        if (suspicionSystem != null)
        {
            suspicionSystem.ClearSuspicion();
            OnSuspicionCleared?.Invoke();
        }

        if (machine.Config.debugStates)
        {
            Debug.Log($"[EnemySuspicious] {machine.gameObject.name} suspicion cleared", machine);
        }
    }

    private void ReturnToNormalBehavior()
    {
        ClearSuspicion();

        // Return to patrol or idle
        if (machine.PatrolRoute != null && machine.PatrolRoute.WaypointCount >= 2)
        {
            machine.SetState(new EnemyPatrolState(machine));
        }
        else
        {
            machine.SetState(new EnemyIdleState(machine));
        }
    }

    public override void Exit()
    {
        machine.Animation.SetAlert(false);
    }
}