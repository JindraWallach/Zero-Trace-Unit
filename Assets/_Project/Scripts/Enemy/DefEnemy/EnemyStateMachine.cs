using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Central orchestrator pro enemy AI.
/// Řídí přechody stavů a drží reference na všechny komponenty.
///
/// FIX 3 – Path Blocked Alarm:
///   Přidána reference na SecurityAlarmSystem (auto-find v Start).
///   EnemyChaseState ji používá pro TriggerAlarm() když je path zablokovaná.
///
/// SRP:  Orchestrace stavů a držení referencí.
/// OOP:  Stavy přistupují k systémům přes gettery, ne přímé fieldy.
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

    // ── FIX 3: Alarm System ───────────────────────────────────────────────────
    [Header("Alarm System")]
    [Tooltip("Auto-found in Start if left empty.")]
    [SerializeField] private SecurityAlarmSystem alarmSystem;

    [Header("Taser Effect")]
    public Transform TaserSpawnPoint;

    [Header("Debug")]
    [SerializeField] private string currentStateName;
    [SerializeField] private float currentSuspicionDebug;

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<EnemyState> OnStateChanged;
    public event Action<Vector3> OnPlayerDetected;
    public event Action<Vector3> OnPlayerLost;
    public event Action<EnemyStateMachine> OnEnemyDestroyed;

    // ── Component references ──────────────────────────────────────────────────
    private EnemyState currentState;
    private EnemyMovementController movementController;
    private EnemyAnimationController animationController;
    private NavMeshAgent navAgent;
    private EnemySuspicionSystem suspicionSystem;
    private EnemyMultiPointVision multiPointVision;

    // ── Memory ────────────────────────────────────────────────────────────────
    private Vector3 lastKnownPlayerPosition;
    private float timeSinceLastSeen;
    private bool hasSeenPlayer;

    // ── Public API ────────────────────────────────────────────────────────────
    public EnemyConfig Config => config;
    public PatrolRoute PatrolRoute => patrolRoute;
    public Transform PlayerTransform => playerTransform;
    public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    public bool HasSeenPlayer => hasSeenPlayer;
    public float TimeSinceLastSeen => timeSinceLastSeen;
    public EnemyMovementController Movement => movementController;
    public EnemyAnimationController Animation => animationController;
    public NavMeshAgent Agent => navAgent;
    public EnemySuspicionSystem Suspicion => suspicionSystem;
    public EnemyMultiPointVision MultiPointVision => multiPointVision;
    public EnemyState CurrentState => currentState;

    /// <summary>FIX 3 – Alarm system getter pro EnemyChaseState.</summary>
    public SecurityAlarmSystem AlarmSystem => alarmSystem;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        movementController = GetComponent<EnemyMovementController>();
        animationController = GetComponent<EnemyAnimationController>();
        suspicionSystem = GetComponent<EnemySuspicionSystem>();
        multiPointVision = GetComponent<EnemyMultiPointVision>();

        if (config == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name}: missing EnemyConfig!", this);
            enabled = false; return;
        }

        if (!config.enableSuspicionSystem)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name}: suspicion system disabled!", this);
            enabled = false; return;
        }

        if (config.suspicionConfig == null)
        {
            Debug.LogError($"[EnemyStateMachine] {gameObject.name}: missing SuspicionConfig!", this);
            enabled = false; return;
        }

        // Auto-find player
        if (playerTransform == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
            else { Debug.LogError($"[EnemyStateMachine] {gameObject.name}: player not found!", this); enabled = false; return; }
        }

        if (suspicionSystem == null)
        {
            Debug.LogWarning($"[EnemyStateMachine] {gameObject.name}: adding EnemySuspicionSystem.", this);
            suspicionSystem = gameObject.AddComponent<EnemySuspicionSystem>();
        }
    }

    private void Start()
    {
        // FIX 3: Auto-find alarm system
        if (alarmSystem == null)
            alarmSystem = FindFirstObjectByType<SecurityAlarmSystem>();

        if (alarmSystem == null)
            Debug.LogWarning($"[EnemyStateMachine] {gameObject.name}: SecurityAlarmSystem not found – path-blocked alarm won't fire.", this);

        // Init components
        movementController.Initialize(this);
        animationController.Initialize(this);
        suspicionSystem.Initialize(this, config.suspicionConfig);
        multiPointVision.Initialize(this, config, config.suspicionConfig, playerTransform);

        // Vision events
        multiPointVision.OnPlayerSpotted += HandlePlayerSpotted;
        multiPointVision.OnPlayerLostSight += HandlePlayerLostSight;

        // Suspicion events
        suspicionSystem.OnAlertTriggered += HandleSuspicionAlert;
        suspicionSystem.OnChaseTriggered += HandleSuspicionChase;
        suspicionSystem.OnSuspicionCleared += HandleSuspicionCleared;

        // Initial state
        SetState(patrolRoute != null && patrolRoute.WaypointCount >= 2
            ? (EnemyState)new EnemyPatrolState(this)
            : new EnemyIdleState(this));

        if (config.debugStates)
            Debug.Log($"[EnemyStateMachine] {gameObject.name} initialized.", this);
    }

    private void Update()
    {
        if (hasSeenPlayer) timeSinceLastSeen += Time.deltaTime;

        currentState?.Update();

        currentSuspicionDebug = suspicionSystem.Suspicion;
    }

    private void OnDestroy()
    {
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

        OnEnemyDestroyed?.Invoke(this);
    }

    // ── State management ──────────────────────────────────────────────────────

    public void SetState(EnemyState newState)
    {
        if (newState == null) { Debug.LogError("[EnemyStateMachine] Null state!", this); return; }

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

    // ── Event handlers ────────────────────────────────────────────────────────

    private void HandlePlayerSpotted(Vector3 pos)
    {
        UpdateLastKnownPosition(pos);
        OnPlayerDetected?.Invoke(pos);
        currentState?.OnPlayerDetected(pos);
    }

    private void HandlePlayerLostSight(Vector3 pos)
    {
        UpdateLastKnownPosition(pos);
        OnPlayerLost?.Invoke(pos);
        currentState?.OnPlayerLost(pos);
    }

    private void HandleSuspicionAlert()
    {
        if (currentState is EnemyPatrolState || currentState is EnemyIdleState)
            SetState(new EnemyAlertState(this, lastKnownPlayerPosition));
    }

    private void HandleSuspicionChase()
    {
        if (currentState is not (EnemyChaseState or EnemyCatchState))
            SetState(new EnemyChaseState(this));
    }

    private void HandleSuspicionCleared()
    {
        if (currentState is EnemyAlertState || currentState is EnemySearchState)
        {
            SetState(patrolRoute != null && patrolRoute.WaypointCount >= 2
                ? (EnemyState)new EnemyPatrolState(this)
                : new EnemyIdleState(this));
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (config == null || !config.debugStates || !hasSeenPlayer) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
        Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
    }
}