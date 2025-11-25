using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Central orchestrator for enemy AI behavior.
/// Pattern identical to DoorStateMachine - manages state transitions.
/// Provides access to all AI components (Movement, Vision, Animation).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyMovementController))]
[RequireComponent(typeof(EnemyVisionDetector))]
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

    // Events for external listeners (UI, analytics, etc.)
    public event Action<EnemyState> OnStateChanged;
    public event Action<Vector3> OnPlayerDetected;
    public event Action<Vector3> OnPlayerLost;
    public event Action OnPlayerCaught;

    // Component references (cached for performance)
    private EnemyState currentState;
    private EnemyMovementController movementController;
    private EnemyVisionDetector visionDetector;
    private EnemyAnimationController animationController;
    private NavMeshAgent navAgent;

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
    public EnemyVisionDetector Vision => visionDetector;
    public EnemyAnimationController Animation => animationController;
    public NavMeshAgent Agent => navAgent;

    // Current state accessor (read-only)
    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        // Cache components
        navAgent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<EnemyMovementController>();
        visionDetector = GetComponent<EnemyVisionDetector>();
        animationController = GetComponent<EnemyAnimationController>();

        // Validate configuration
        if (config == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name} missing EnemyConfig!", this);
            enabled = false;
            return;
        }

        // Note: Do NOT set initial state here — component initialization (Movement/Vision/Animation)
        // that the states rely on happens in Start(). Setting a state in Awake() can call state.Enter()
        // which may call Movement.MoveToPosition before Movement.Initialize has run and lead to NRE.
    }

    private void Start()
    {
        // Initialize components with config
        movementController.Initialize(this);
        visionDetector.Initialize(this);
        animationController.Initialize(this);

        // Subscribe to vision events
        visionDetector.OnPlayerSpotted += HandlePlayerSpotted;
        visionDetector.OnPlayerLostSight += HandlePlayerLostSight;

        // Start in appropriate state (after components are initialized)
        if (patrolRoute != null && patrolRoute.WaypointCount >= 2)
            SetState(new EnemyPatrolState(this));
        else
            SetState(new EnemyIdleState(this));
    }

    private void Update()
    {
        // Update memory system
        if (hasSeenPlayer)
            timeSinceLastSeen += Time.deltaTime;

        // Update current state
        currentState?.Update();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (visionDetector != null)
        {
            visionDetector.OnPlayerSpotted -= HandlePlayerSpotted;
            visionDetector.OnPlayerLostSight -= HandlePlayerLostSight;
        }
    }

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
    }

    /// <summary>
    /// Called when player is caught (triggers game over).
    /// </summary>
    public void CatchPlayer()
    {
        OnPlayerCaught?.Invoke();

        if (showDebugLogs)
            Debug.Log($"[EnemyStateMachine] {gameObject.name} caught player!", this);

        // Notify GameManager
        GameManager.Instance?.OnPlayerCaught();
    }

    // === Event Handlers ===

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

    // === Debug Gizmos ===

    private void OnDrawGizmosSelected()
    {
        if (config == null) return;

        // Draw vision cone (forwarded to VisionDetector)
        if (config.debugVision && visionDetector != null)
        {
            visionDetector.DrawVisionGizmos();
        }

        // Draw last known position
        if (hasSeenPlayer && config.debugStates)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
        }

        // Draw state-specific debug (if state implements it)
        // States can override DrawGizmos() method if needed
    }
}