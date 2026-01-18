using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages suspicion meter (0-100%) with gradual detection.
/// Replaces instant spot → chase with smooth build-up.
/// SRP: Only handles suspicion calculation and state changes.
/// Performance: Coroutine-based updates (no Update() spam).
/// ALL SETTINGS FROM SO - no Inspector overrides.
/// </summary>
public class EnemySuspicionSystem : MonoBehaviour
{
    // Events
    public event Action<float> OnSuspicionChanged;
    public event Action OnAlertTriggered;
    public event Action OnChaseTriggered;
    public event Action OnSuspicionCleared;

    // References (set by EnemyStateMachine)
    private SuspicionConfig config;
    private EnemyStateMachine stateMachine;

    // State
    private float currentSuspicion = 0f;
    private bool isPlayerVisible;
    private int visibleBodyParts;
    private float timeSinceLastSeen;

    // Threshold tracking (prevent event spam)
    private bool wasAlert;
    private bool wasChasing;

    // Coroutine
    private Coroutine updateCoroutine;

    // Public API
    public float Suspicion => currentSuspicion;
    public float SuspicionPercent => currentSuspicion;
    public bool IsAlert => currentSuspicion >= config.alertThreshold;
    public bool ShouldChase => currentSuspicion >= config.chaseThreshold;
    public bool IsPlayerVisible => isPlayerVisible;

    public void Initialize(EnemyStateMachine machine, SuspicionConfig suspicionConfig)
    {
        stateMachine = machine;
        config = suspicionConfig;

        if (config == null)
        {
            Debug.LogError($"[EnemySuspicionSystem] {gameObject.name} missing SuspicionConfig!", this);
            enabled = false;
            return;
        }

        // Ensure tracking coroutine is running after initialization
        StartTracking();
    }

    private void OnEnable()
    {
        if (config != null)
            StartTracking();
    }

    private void OnDisable()
    {
        StopTracking();
    }

    /// <summary>
    /// Call this when player visibility changes (from MultiPointVision).
    /// </summary>
    public void SetPlayerVisible(bool visible, int visibleParts)
    {
        isPlayerVisible = visible;
        visibleBodyParts = Mathf.Clamp(visibleParts, 0, 4);

        if (visible)
        {
            timeSinceLastSeen = 0f;
            // Notify listeners immediately so UI can reflect current suspicion right away
            OnSuspicionChanged?.Invoke(currentSuspicion);
        }
    }

    /// <summary>
    /// Manually increase suspicion (e.g., from noise detection).
    /// </summary>
    public void AddSuspicion(float amount)
    {
        currentSuspicion = Mathf.Clamp(currentSuspicion + amount, 0f, 100f);
        CheckThresholds();
        OnSuspicionChanged?.Invoke(currentSuspicion);
    }

    /// <summary>
    /// Reset suspicion to 0 (used when enemy gives up search).
    /// </summary>
    public void ClearSuspicion()
    {
        currentSuspicion = 0f;
        wasAlert = false;
        wasChasing = false;
        OnSuspicionChanged?.Invoke(0f);
        OnSuspicionCleared?.Invoke();
    }

    // === INTERNAL LOGIC ===

    private void StartTracking()
    {
        StopTracking();
        updateCoroutine = StartCoroutine(SuspicionUpdateCoroutine());
    }

    private void StopTracking()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    private IEnumerator SuspicionUpdateCoroutine()
    {
        var wait = new WaitForSeconds(config.updateInterval);

        while (true)
        {
            if (isPlayerVisible)
            {
                // Build suspicion (exponential based on visible parts)
                float effectiveRate = config.GetEffectiveBuildRate(visibleBodyParts);
                currentSuspicion += effectiveRate * config.updateInterval;
                currentSuspicion = Mathf.Min(currentSuspicion, 100f);

                CheckThresholds();
                OnSuspicionChanged?.Invoke(currentSuspicion);
            }
            else
            {
                // Grace period before decay
                timeSinceLastSeen += config.updateInterval;

                if (timeSinceLastSeen >= config.decayGracePeriod)
                {
                    // Decay suspicion
                    currentSuspicion -= config.decayRate * config.updateInterval;
                    currentSuspicion = Mathf.Max(currentSuspicion, 0f);

                    CheckThresholds();
                    OnSuspicionChanged?.Invoke(currentSuspicion);

                    if (currentSuspicion <= 0f)
                        OnSuspicionCleared?.Invoke();
                }
            }

            yield return wait;
        }
    }

    private void CheckThresholds()
    {
        // Alert threshold
        bool isAlert = currentSuspicion >= config.alertThreshold;
        if (isAlert && !wasAlert)
        {
            wasAlert = true;
            OnAlertTriggered?.Invoke();
        }
        else if (!isAlert && wasAlert)
        {
            wasAlert = false;
        }

        // Chase threshold
        bool shouldChase = currentSuspicion >= config.chaseThreshold;
        if (shouldChase && !wasChasing)
        {
            wasChasing = true;
            OnChaseTriggered?.Invoke();
        }
        else if (!shouldChase && wasChasing)
        {
            wasChasing = false;
        }
    }

    // === DEBUG GIZMOS (Only suspicion bar) ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (config == null || !config.showDebugBar)
            return;

        DrawSuspicionGizmo();
    }

    private void OnDrawGizmos()
    {
        if (config == null || !config.showDebugBar)
            return;

        DrawSuspicionGizmo();
    }

    private void DrawSuspicionGizmo()
    {
        // Suspicion bar above enemy head
        Vector3 barPosition = transform.position + Vector3.up * 2.5f;
        float barWidth = 2f;
        float barHeight = 0.2f;

        // Background (dark gray)
        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.05f));

        // Foreground (color based on suspicion)
        float fillPercent = currentSuspicion / 100f;
        Color barColor = GetSuspicionColor(fillPercent);

        Gizmos.color = barColor;
        Vector3 fillSize = new Vector3(barWidth * fillPercent, barHeight, 0.08f);
        Vector3 fillOffset = new Vector3(-barWidth * (1f - fillPercent) * 0.5f, 0f, 0f);
        Gizmos.DrawCube(barPosition + fillOffset, fillSize);

        // Label
        if (config.showSuspicionValue)
        {
            string status = ShouldChase ? "CHASE" : IsAlert ? "ALERT" : "PATROL";
            string label = $"{currentSuspicion:F0}% | {status}";
            if (isPlayerVisible)
                label += $"\n{visibleBodyParts}/4 visible";

            UnityEditor.Handles.Label(
                barPosition + Vector3.up * 0.6f,
                label,
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = barColor },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                }
            );
        }
    }

    private Color GetSuspicionColor(float percent)
    {
        if (percent < 0.3f)
            return Color.Lerp(Color.green, Color.yellow, percent / 0.3f);
        else if (percent < 0.7f)
            return Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (percent - 0.3f) / 0.4f); // orange
        else
            return Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, (percent - 0.7f) / 0.3f);
    }
#endif
}