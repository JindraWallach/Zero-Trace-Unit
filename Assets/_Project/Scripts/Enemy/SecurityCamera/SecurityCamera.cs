using System;
using UnityEngine;

/// <summary>
/// Security camera state machine with gradual detection system.
/// Detects player, builds suspicion meter (0-100%), triggers alert at 100%.
/// Uses switch-based state updates for performance.
/// </summary>
[RequireComponent(typeof(SecurityCameraVision))]
public class SecurityCamera : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private SecurityCameraConfig config;

    [Header("Player Reference")]
    [SerializeField] private Transform player;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Alarm System")]
    [SerializeField] private SecurityAlarmSystem alarmSystem;

    [Header("Debug - Current State")]
    [SerializeField] private CameraState currentStateDebug;
    [SerializeField] private float suspicionMeterDebug;
    [SerializeField] private bool canSeePlayerDebug;

    // Events
    public event Action OnAlertTriggered;
    public event Action<float> OnSuspicionChanged;

    // Components
    private SecurityCameraVision vision;
    private SecurityCameraIndicator indicator;

    // State
    private CameraState currentState;
    private float suspicionMeter; // 0-100
    private float stateTimer;

    // Cached calculations
    private float suspicionBuildRate;
    private float suspicionDecayRate;

    // Public API
    public CameraState CurrentState => currentState;
    public float SuspicionMeter => suspicionMeter;
    public bool IsAlerting => currentState == CameraState.Alert;

    private void Awake()
    {
        // Cache components
        vision = GetComponent<SecurityCameraVision>();
        indicator = GetComponent<SecurityCameraIndicator>();

        // Validate config
        if (config == null)
        {
            Debug.LogError($"[SecurityCamera] {name} missing SecurityCameraConfig!", this);
            enabled = false;
            return;
        }

        // Auto-find player if needed
        if (autoFindPlayer && player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning($"[SecurityCamera] {name} could not find player with tag 'Player'", this);
            }
        }
    }

    private void Start()
    {
        // Initialize vision
        vision.Initialize(config, player);

        if (alarmSystem == null)
            alarmSystem = FindFirstObjectByType<SecurityAlarmSystem>();

        // Calculate rates once
        suspicionBuildRate = 100f / config.suspicionBuildTime;
        suspicionDecayRate = 100f / config.suspicionDecayTime;

        // Start in Idle state
        TransitionToState(CameraState.Idle);

        // Register with HUD
        if (SecurityCameraHUD.Instance != null)
        {
            SecurityCameraHUD.Instance.RegisterCamera(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from HUD
        if (SecurityCameraHUD.Instance != null)
        {
            SecurityCameraHUD.Instance.UnregisterCamera(this);
        }
    }

    private void Update()
    {
        // State machine update
        switch (currentState)
        {
            case CameraState.Idle:
                UpdateIdleState();
                break;

            case CameraState.Suspicious:
                UpdateSuspiciousState();
                break;

            case CameraState.Alert:
                UpdateAlertState();
                break;
        }

#if UNITY_EDITOR
        UpdateDebugInfo();
#endif
    }

    // === STATE UPDATE METHODS ===

    private void UpdateIdleState()
    {
        // Check if player enters FOV
        if (vision.CanSeePlayer())
        {
            TransitionToState(CameraState.Suspicious);
        }
    }

    private void UpdateSuspiciousState()
    {
        bool canSeePlayer = vision.CanSeePlayer();

        if (canSeePlayer)
        {
            // Build suspicion
            suspicionMeter += suspicionBuildRate * Time.deltaTime;
            suspicionMeter = Mathf.Min(suspicionMeter, 100f);

            // Fire suspicion changed event
            OnSuspicionChanged?.Invoke(suspicionMeter);

            // Check if reached 100%
            if (suspicionMeter >= 100f)
            {
                TransitionToState(CameraState.Alert);
            }
        }
        else
        {
            // Decay suspicion
            suspicionMeter -= suspicionDecayRate * Time.deltaTime;
            suspicionMeter = Mathf.Max(suspicionMeter, 0f);

            // Fire suspicion changed event
            OnSuspicionChanged?.Invoke(suspicionMeter);

            // Check if fully decayed
            if (suspicionMeter <= 0f)
            {
                TransitionToState(CameraState.Idle);
            }
        }
    }

    private void UpdateAlertState()
    {
        stateTimer += Time.deltaTime;

        // Return to idle after cooldown
        if (stateTimer >= config.alertCooldown)
        {
            // TODO: When AlarmSystem implemented, remove this reset
            // Currently resets immediately - with AlarmSystem, camera should stay red
            // until alarm is cancelled by player or times out

            // Hide HUD warning
            if (SecurityCameraHUD.Instance != null)
            {
                SecurityCameraHUD.Instance.HideWarning();
                //SecurityCameraHUD.Instance.UpdateSuspicionBar(0f);
            }

            TransitionToState(CameraState.Idle);
        }
    }

    // === STATE TRANSITION ===

    private void TransitionToState(CameraState newState)
    {
        // Exit current state
        OnStateExit(currentState);

        // Change state
        CameraState oldState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // Enter new state
        OnStateEnter(newState);

        // Debug logging
        if (config.debugStates)
        {
            Debug.Log($"[SecurityCamera] {name} state: {oldState} → {newState}", this);
        }
    }

    private void OnStateEnter(CameraState state)
    {
        switch (state)
        {
            case CameraState.Idle:
                suspicionMeter = 0f;
                indicator.SetState(CameraState.Idle);
                break;

            case CameraState.Suspicious:
                indicator.SetState(CameraState.Suspicious);
                break;

            case CameraState.Alert:
                indicator.SetState(CameraState.Alert);
                TriggerAlert();
                break;
        }
    }

    private void OnStateExit(CameraState state)
    {
        // Cleanup per state if needed
        switch (state)
        {
            case CameraState.Idle:
                break;

            case CameraState.Suspicious:
                break;

            case CameraState.Alert:
                break;
        }
    }

    // === ALERT SYSTEM ===

    private void TriggerAlert()
    {
        // Fire event
        OnAlertTriggered?.Invoke();

        if (config.debugStates)
        {
            Debug.Log($"[SecurityCamera] {name} ALERT TRIGGERED at position {transform.position}", this);
        }

        // Trigger alarm system
        if (alarmSystem != null)
        {
            alarmSystem.TriggerAlarm(transform.position);
        }
        else
        {
            Debug.LogError($"[SecurityCamera] {name} No SecurityAlarmSystem found!", this);
        }
    }

    // === PUBLIC API ===

    /// <summary>
    /// Manually reset camera to idle state (for testing/hacking).
    /// </summary>
    public void ResetCamera()
    {
        TransitionToState(CameraState.Idle);
    }

    /// <summary>
    /// Force camera into alert state (for testing).
    /// </summary>
    public void ForceAlert()
    {
        suspicionMeter = 100f;
        TransitionToState(CameraState.Alert);
    }

    /// <summary>
    /// Set player reference dynamically.
    /// </summary>
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        if (vision != null)
        {
            vision.Initialize(config, player);
        }
    }

    // === DEBUG ===

    private void UpdateDebugInfo()
    {
        currentStateDebug = currentState;
        suspicionMeterDebug = suspicionMeter;
        canSeePlayerDebug = vision != null && vision.CanSeePlayer();
    }

    private void OnDrawGizmosSelected()
    {
        if (config != null && config.debugVision && vision != null)
        {
            vision.DrawVisionGizmos();
        }

        // Draw suspicion meter as wire sphere size
        if (Application.isPlaying && currentState == CameraState.Suspicious)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, suspicionMeter / 100f);
            float radius = 0.5f + (suspicionMeter / 100f) * 1f;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}