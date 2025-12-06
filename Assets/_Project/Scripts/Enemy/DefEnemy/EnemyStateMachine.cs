using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Central orchestrator for enemy AI behavior.
/// NOW WITH: Suspicion system integration for gradual detection.
/// Pattern identical to DoorStateMachine - manages state transitions.
/// Provides access to all AI components (Movement, Vision, Animation, Suspicion).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyMovementController))]
[RequireComponent(typeof(EnemyAnimationController))]
public class EnemyStateMachine : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private EnemyConfig config;
    [SerializeField] private PatrolRoute patrolRoute;

    [Header("References")]
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [SerializeField] private string currentStateName;
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private float currentSuspicion;

    // Events for external listeners (UI, analytics, etc.)
    public event Action<EnemyState> OnStateChanged;
    public event Action<Vector3> OnPlayerDetected;
    public event Action<Vector3> OnPlayerLost;

    // Component references (cached for performance)
    private EnemyState currentState;
    private EnemyMovementController movementController;
    private EnemyAnimationController animationController;
    private NavMeshAgent navAgent;

    // NEW: Suspicion system components
    private EnemySuspicionSystem suspicionSystem;
    private EnemyMultiPointVision multiPointVision;
    private EnemyVisionDetector legacyVision; // Fallback for old system

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

    // NEW: Suspicion system accessors
    public EnemySuspicionSystem Suspicion => suspicionSystem;
    public EnemyMultiPointVision MultiPointVision => multiPointVision;
    public bool UsingSuspicionSystem => config != null && config.enableSuspicionSystem && suspicionSystem != null;

    // Current state accessor (read-only)
    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        // Cache components
        navAgent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<EnemyMovementController>();
        animationController = GetComponent<EnemyAnimationController>();

        // NEW: Get suspicion system components (may be null if not using)
        suspicionSystem = GetComponent<EnemySuspicionSystem>();
        multiPointVision = GetComponent<EnemyMultiPointVision>();
        legacyVision = GetComponent<EnemyVisionDetector>();

        // Validate configuration
        if (config == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name} missing EnemyConfig!", this);
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
                Debug.Log($"[EnemyStateMachine] {gameObject.name} auto-found player", this);
            }
        }
    }

    private void Start()
    {
        // Initialize components with config
        movementController.Initialize(this);
        animationController.Initialize(this);

        // Initialize vision system based on config
        if (config.enableSuspicionSystem && config.enableMultiPointVision)
        {
            InitializeMultiPointVisionSystem();
        }
        else
        {
            InitializeLegacyVisionSystem();
        }

        // Start in appropriate state (after components are initialized)
        if (patrolRoute != null && patrolRoute.WaypointCount >= 2)
            SetState(new EnemyPatrolState(this));
        else
            SetState(new EnemyIdleState(this));

        Debug.Log($"[EnemyStateMachine] {gameObject.name} initialized with {(UsingSuspicionSystem ? "SUSPICION" : "LEGACY")} system", this);
    }

    private void Update()
    {
        // Update memory system
        if (hasSeenPlayer)
            timeSinceLastSeen += Time.deltaTime;

        // Update current state
        currentState?.Update();

        // Debug info
        if (suspicionSystem != null)
            currentSuspicion = suspicionSystem.Suspicion;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        UnsubscribeFromVisionEvents();
        UnsubscribeFromSuspicionEvents();
    }

    // === VISION SYSTEM INITIALIZATION ===

    private void InitializeMultiPointVisionSystem()
    {
        if (multiPointVision == null)
        {
            Debug.LogWarning($"[EnemyStateMachine] {gameObject.name} missing EnemyMultiPointVision component! Add it or disable multi-point vision in config.", this);
            InitializeLegacyVisionSystem();
            return;
        }

        if (suspicionSystem == null)
        {
            Debug.LogWarning($"[EnemyStateMachine] {gameObject.name} missing EnemySuspicionSystem component! Add it or disable suspicion system in config.", this);
            InitializeLegacyVisionSystem();
            return;
        }

        // Initialize multi-point vision
        multiPointVision.Initialize(this, playerTransform);

        // Subscribe to events
        multiPointVision.OnPlayerSpotted += HandlePlayerSpotted;
        multiPointVision.OnPlayerLostSight += HandlePlayerLostSight;

        // Subscribe to suspicion events
        suspicionSystem.OnAlertTriggered += HandleSuspicionAlert;
        suspicionSystem.OnChaseTriggered += HandleSuspicionChase;
        suspicionSystem.OnSuspicionCleared += HandleSuspicionCleared;

        Debug.Log($"[EnemyStateMachine] {gameObject.name} using MULTI-POINT VISION + SUSPICION system", this);
    }

    private void InitializeLegacyVisionSystem()
    {
        if (legacyVision == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name} missing EnemyVisionDetector component!", this);
            return;
        }

        legacyVision.Initialize(this);

        // Subscribe to legacy vision events
        legacyVision.OnPlayerSpotted += HandlePlayerSpotted;
        legacyVision.OnPlayerLostSight += HandlePlayerLostSight;

        Debug.Log($"[EnemyStateMachine] {gameObject.name} using LEGACY VISION system (instant detection)", this);
    }

    private void UnsubscribeFromVisionEvents()
    {
        if (multiPointVision != null)
        {
            multiPointVision.OnPlayerSpotted -= HandlePlayerSpotted;
            multiPointVision.OnPlayerLostSight -= HandlePlayerLostSight;
        }

        if (legacyVision != null)
        {
            legacyVision.OnPlayerSpotted -= HandlePlayerSpotted;
            legacyVision.OnPlayerLostSight -= HandlePlayerLostSight;
        }
    }

    private void UnsubscribeFromSuspicionEvents()
    {
        if (suspicionSystem != null)
        {
            suspicionSystem.OnAlertTriggered -= HandleSuspicionAlert;
            suspicionSystem.OnChaseTriggered -= HandleSuspicionChase;
            suspicionSystem.OnSuspicionCleared -= HandleSuspicionCleared;
        }
    }

    // === STATE MANAGEMENT ===

    /// <summary>
    /// Transitions to a new state (public API).
    /// Handles Enter/Exit lifecycle + debug logging.
    /// </summary>
    public void SetState(EnemyState newState)
    {
        if (newState == null)
        {
            Debug.LogError("[EnemyStateMachine] Attempted to set null state!", this);
            return;
        }

        // Exit previous state
        currentState?.Exit();

        // Set new state
        currentState = newState;
        currentStateName = currentState.GetType().Name;

        // Debug logging
        if (showDebugLogs && config.debugStates)
            Debug.Log($"[EnemyStateMachine] {gameObject.name} → {currentStateName}", this);

        // Enter new state
        currentState.Enter();

        // Notify listeners
        OnStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Updates last known player position (memory system).
    /// </summary>
    public void UpdateLastKnownPosition(Vector3 position)
    {
        lastKnownPlayerPosition = position;
        timeSinceLastSeen = 0f;
        hasSeenPlayer = true;
    }

    /// <summary>
    /// Clears player memory (used when returning to patrol).
    /// </summary>
    public void ClearMemory()
    {
        hasSeenPlayer = false;
        timeSinceLastSeen = 0f;

        // Also clear suspicion if using system
        if (suspicionSystem != null)
            suspicionSystem.ClearSuspicion();
    }

    // === EVENT HANDLERS ===

    private void HandlePlayerSpotted(Vector3 playerPosition)
    {
        UpdateLastKnownPosition(playerPosition);
        OnPlayerDetected?.Invoke(playerPosition);

        // Let state handle detection (different behavior per state)
        currentState?.OnPlayerDetected(playerPosition);
    }

    private void HandlePlayerLostSight(Vector3 lastPosition)
    {
        UpdateLastKnownPosition(lastPosition);
        OnPlayerLost?.Invoke(lastPosition);

        // Let state handle losing player
        currentState?.OnPlayerLost(lastPosition);
    }

    // NEW: Suspicion system event handlers
    private void HandleSuspicionAlert()
    {
        // 30%+ suspicion reached → transition to Alert state
        if (currentState is EnemyPatrolState || currentState is EnemyIdleState)
        {
            SetState(new EnemyAlertState(this, lastKnownPlayerPosition));
        }
    }

    private void HandleSuspicionChase()
    {
        // 100% suspicion reached → transition to Chase state
        if (!(currentState is EnemyChaseState || currentState is EnemyAttackState || currentState is EnemyCatchState))
        {
            SetState(new EnemyChaseState(this));
        }
    }

    private void HandleSuspicionCleared()
    {
        // Suspicion dropped to 0% → return to patrol/idle
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

        // Draw vision cone (forwarded to active vision system)
        if (config.debugVision)
        {
            if (multiPointVision != null && config.enableMultiPointVision)
            {
                // Multi-point vision draws its own gizmos
            }
            else if (legacyVision != null)
            {
                legacyVision.DrawVisionGizmos();
            }
        }

        // Draw last known position
        if (hasSeenPlayer && config.debugStates)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }
    }
}