using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Central orchestrator for enemy AI behavior.
/// Manages state transitions with suspicion system.
/// NO LEGACY SYSTEM - clean suspicion-only implementation.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyMovementController))]
[RequireComponent(typeof(EnemyAnimationController))]
[RequireComponent(typeof(EnemySuspicionSystem))]
[RequireComponent(typeof(EnemyMultiPointVision))]
public class EnemyStateMachine : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyConfig config;
    [SerializeField] private PatrolRoute patrolRoute;

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [SerializeField] private string currentStateName;
    [SerializeField] private float currentSuspicionDebug;

    // Events
    public event Action<EnemyState> OnStateChanged;
    public event Action<Vector3> OnPlayerDetected;
    public event Action<Vector3> OnPlayerLost;

    // Component references
    private EnemyState currentState;
    private EnemyMovementController movementController;
    private EnemyAnimationController animationController;
    private NavMeshAgent navAgent;
    private EnemySuspicionSystem suspicionSystem;
    private EnemyMultiPointVision multiPointVision;

    // Memory system
    private Vector3 lastKnownPlayerPosition;
    private float timeSinceLastSeen;
    private bool hasSeenPlayer;

    // Public API for states
    public EnemyConfig Config => config;
    public PatrolRoute PatrolRoute => patrolRoute;
    public Transform PlayerTransform => playerTransform;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    public bool HasSeenPlayer => hasSeenPlayer;
    public float TimeSinceLastSeen => timeSinceLastSeen;

    // Component accessors
    public EnemyMovementController Movement => movementController;
    public EnemyAnimationController Animation => animationController;
    public NavMeshAgent Agent => navAgent;
    public EnemySuspicionSystem Suspicion => suspicionSystem;
    public EnemyMultiPointVision MultiPointVision => multiPointVision;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        // Cache components
        navAgent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<EnemyMovementController>();
        animationController = GetComponent<EnemyAnimationController>();
        suspicionSystem = GetComponent<EnemySuspicionSystem>();
        multiPointVision = GetComponent<EnemyMultiPointVision>();

        // Validate configuration
        if (config == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name} missing EnemyConfig!", this);
            enabled = false;
            return;
        }

        if (!config.enableSuspicionSystem)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name} suspicion system disabled in config! This system requires it.", this);
            enabled = false;
            return;
        }

        if (config.suspicionConfig == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name} missing SuspicionConfig reference in EnemyConfig!", this);
            enabled = false;
            return;
        }

        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError($"[EnemyStateMachine] {gameObject.name} could not find player!", this);
                enabled = false;
                return;
            }
        }
    }

    private void Start()
    {
        // Initialize components
        movementController.Initialize(this);
        animationController.Initialize(this);
        suspicionSystem.Initialize(this, config.suspicionConfig);
        multiPointVision.Initialize(this, config, config.suspicionConfig, playerTransform);

        // Subscribe to vision events
        multiPointVision.OnPlayerSpotted += HandlePlayerSpotted;
        multiPointVision.OnPlayerLostSight += HandlePlayerLostSight;

        // Subscribe to suspicion events
        suspicionSystem.OnAlertTriggered += HandleSuspicionAlert;
        suspicionSystem.OnChaseTriggered += HandleSuspicionChase;
        suspicionSystem.OnSuspicionCleared += HandleSuspicionCleared;

        // Start in appropriate state
        if (patrolRoute != null && patrolRoute.WaypointCount >= 2)
            SetState(new EnemyPatrolState(this));
        else
            SetState(new EnemyIdleState(this));

        Debug.Log($"[EnemyStateMachine] {gameObject.name} initialized with suspicion system", this);
    }

    private void Update()
    {
        // Update memory system
        if (hasSeenPlayer)
            timeSinceLastSeen += Time.deltaTime;

        // Update current state
        currentState?.Update();

        // Debug info
        currentSuspicionDebug = suspicionSystem.Suspicion;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (multiPointVision != null)
        {
            multiPointVision.OnPlayerSpotted -= HandlePlayerSpotted;
            multiPointVision.OnPlayerLostSight -= HandlePlayerLostSight;
        }

        if (suspicionSystem != null)
        {
            suspicionSystem.OnAlertTriggered -= HandleSuspicionAlert;
            suspicionSystem.OnChaseTriggered -= HandleSuspicionChase;
            suspicionSystem.OnSuspicionCleared -= HandleSuspicionCleared;
        }
    }

    // === STATE MANAGEMENT ===

    public void SetState(EnemyState newState)
    {
        if (newState == null)
        {
            Debug.LogError("[EnemyStateMachine] Attempted to set null state!", this);
            return;
        }

        currentState?.Exit();
        currentState = newState;
        currentStateName = currentState.GetType().Name;

        if (config.debugStates)
            Debug.Log($"[EnemyStateMachine] {gameObject.name} → {currentStateName}", this);

        currentState.Enter();
        OnStateChanged?.Invoke(currentState);
    }

    public void UpdateLastKnownPosition(Vector3 position)
    {
        lastKnownPlayerPosition = position;
        timeSinceLastSeen = 0f;
        hasSeenPlayer = true;
    }

    public void ClearMemory()
    {
        hasSeenPlayer = false;
        timeSinceLastSeen = 0f;
        suspicionSystem.ClearSuspicion();
    }

    // === EVENT HANDLERS ===

    private void HandlePlayerSpotted(Vector3 playerPosition)
    {
        UpdateLastKnownPosition(playerPosition);
        OnPlayerDetected?.Invoke(playerPosition);
        currentState?.OnPlayerDetected(playerPosition);
    }

    private void HandlePlayerLostSight(Vector3 lastPosition)
    {
        UpdateLastKnownPosition(lastPosition);
        OnPlayerLost?.Invoke(lastPosition);
        currentState?.OnPlayerLost(lastPosition);
    }

    private void HandleSuspicionAlert()
    {
        // 30%+ suspicion → Alert state
        if (currentState is EnemyPatrolState || currentState is EnemyIdleState)
        {
            SetState(new EnemyAlertState(this, lastKnownPlayerPosition));
        }
    }

    private void HandleSuspicionChase()
    {
        // 100% suspicion → Chase state
        if (!(currentState is EnemyChaseState || currentState is EnemyAttackState || currentState is EnemyCatchState))
        {
            SetState(new EnemyChaseState(this));
        }
    }

    private void HandleSuspicionCleared()
    {
        // 0% suspicion → return to patrol/idle
        if (currentState is EnemyAlertState || currentState is EnemySearchState)
        {
            if (patrolRoute != null && patrolRoute.WaypointCount >= 2)
                SetState(new EnemyPatrolState(this));
            else
                SetState(new EnemyIdleState(this));
        }
    }

    // === DEBUG GIZMOS ===

    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        // Draw last known position
        if (hasSeenPlayer && config.debugStates)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }
    }
}