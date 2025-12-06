using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages suspicion meter (0-100%) with gradual detection.
/// Replaces instant spot → chase with smooth build-up.
/// SRP: Only handles suspicion calculation and state changes.
/// Performance: Coroutine-based updates (no Update() spam).
/// </summary>
public class EnemySuspicionSystem : MonoBehaviour
{
    [Header("Suspicion Settings")]
    [Tooltip("Suspicion increase per second when player visible (base rate)")]
    [Range(1f, 100f)]
    [SerializeField] private float suspicionBuildRate = 20f;

    [Tooltip("Suspicion decrease per second when player hidden")]
    [Range(1f, 50f)]
    [SerializeField] private float suspicionDecayRate = 10f;

    [Tooltip("Delay before suspicion starts decaying after losing sight")]
    [Range(0f, 5f)]
    [SerializeField] private float decayGracePeriod = 1f;

    [Tooltip("Multiplier per visible body part (0.5 = +50% per part)")]
    [Range(0f, 2f)]
    [SerializeField] private float visibilityMultiplier = 0.5f;

    [Header("Thresholds")]
    [Tooltip("Suspicion % to enter Alert state (30-99%)")]
    [Range(0f, 99f)]
    [SerializeField] private float alertThreshold = 30f;

    [Tooltip("Suspicion % to enter Chase state (100%)")]
    [SerializeField] private float chaseThreshold = 100f;

    [Header("Performance Settings")]
    [Tooltip("How often to update suspicion (seconds) - lower = more responsive but heavier")]
    [SerializeField] private float updateInterval = 0.1f;

    [Tooltip("Enable instant detection mode (legacy behavior, bypasses suspicion)")]
    [SerializeField] private bool enableInstantDetection = false;

    [Header("Debug")]
    [SerializeField] private float currentSuspicion = 0f;
    [SerializeField] private bool isPlayerVisible;
    [SerializeField] private int visibleBodyParts;

    // Events
    public event Action<float> OnSuspicionChanged; // UI updates
    public event Action OnAlertTriggered; // 30%+ reached
    public event Action OnChaseTriggered; // 100% reached
    public event Action OnSuspicionCleared; // Back to 0%

    // State tracking
    private bool wasAlert;
    private bool wasChasing;
    private float timeSinceLastSeen;
    private Coroutine updateCoroutine;

    // Public API
    public float Suspicion => currentSuspicion;
    public float SuspicionPercent => currentSuspicion;
    public bool IsAlert => currentSuspicion >= alertThreshold;
    public bool ShouldChase => currentSuspicion >= chaseThreshold;
    public bool IsPlayerVisible => isPlayerVisible;
    public bool IsInstantDetectionMode => enableInstantDetection;

    private void OnEnable()
    {
        StartTracking();
    }

    private void OnDisable()
    {
        StopTracking();
    }

    /// <summary>
    /// Call this when player becomes visible (from MultiPointVision).
    /// </summary>
    public void SetPlayerVisible(bool visible, int visibleParts)
    {
        isPlayerVisible = visible;
        visibleBodyParts = Mathf.Clamp(visibleParts, 0, 4);

        if (visible)
            timeSinceLastSeen = 0f;
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
        var wait = new WaitForSeconds(updateInterval);

        while (true)
        {
            if (isPlayerVisible)
            {
                // INSTANT DETECTION MODE (legacy/boss enemies)
                if (enableInstantDetection)
                {
                    currentSuspicion = 100f;
                    CheckThresholds();
                    OnSuspicionChanged?.Invoke(currentSuspicion);
                }
                else
                {
                    // Build suspicion gradually
                    float effectiveRate = CalculateBuildRate();
                    currentSuspicion += effectiveRate * updateInterval;
                    currentSuspicion = Mathf.Clamp(currentSuspicion, 0f, 100f);

                    CheckThresholds();
                    OnSuspicionChanged?.Invoke(currentSuspicion);
                }
            }
            else
            {
                // Grace period before decay
                timeSinceLastSeen += updateInterval;

                if (timeSinceLastSeen >= decayGracePeriod)
                {
                    // Decay suspicion
                    currentSuspicion -= suspicionDecayRate * updateInterval;
                    currentSuspicion = Mathf.Max(currentSuspicion, 0f);

                    // Check if dropped below thresholds
                    CheckThresholds();
                    OnSuspicionChanged?.Invoke(currentSuspicion);

                    if (currentSuspicion <= 0f)
                        OnSuspicionCleared?.Invoke();
                }
            }

            yield return wait;
        }
    }

    private float CalculateBuildRate()
    {
        // Base rate + bonus per visible body part
        // Formula: baseRate * (1 + visibleParts * multiplier)
        // Example: 4/4 visible = baseRate * (1 + 4 * 0.5) = 3x faster
        float multiplier = 1f + (visibleBodyParts * visibilityMultiplier);
        return suspicionBuildRate * multiplier;
    }

    private void CheckThresholds()
    {
        // Alert threshold (30%+)
        if (!wasAlert && currentSuspicion >= alertThreshold)
        {
            wasAlert = true;
            OnAlertTriggered?.Invoke();
        }
        else if (wasAlert && currentSuspicion < alertThreshold)
        {
            wasAlert = false;
        }

        // Chase threshold (100%)
        if (!wasChasing && currentSuspicion >= chaseThreshold)
        {
            wasChasing = true;
            OnChaseTriggered?.Invoke();
        }
        else if (wasChasing && currentSuspicion < chaseThreshold)
        {
            wasChasing = false;
        }
    }

    // === DEBUG GIZMOS ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw suspicion bar above enemy (in-scene debug)
        Vector3 barPosition = transform.position + Vector3.up * 2.5f;
        float barWidth = 2f;
        float barHeight = 0.2f;

        // Background (gray)
        Gizmos.color = Color.gray;
        Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.1f));

        // Foreground (color based on suspicion)
        float fillPercent = currentSuspicion / 100f;
        Color barColor = Color.Lerp(Color.green, Color.yellow, fillPercent * 2f);
        if (fillPercent > 0.5f)
            barColor = Color.Lerp(Color.yellow, Color.red, (fillPercent - 0.5f) * 2f);

        Gizmos.color = barColor;
        Vector3 fillSize = new Vector3(barWidth * fillPercent, barHeight, 0.15f);
        Vector3 fillOffset = new Vector3(-barWidth * (1f - fillPercent) * 0.5f, 0f, 0f);
        Gizmos.DrawCube(barPosition + fillOffset, fillSize);

        // Label
        UnityEditor.Handles.Label(
            barPosition + Vector3.up * 0.3f,
            $"Suspicion: {currentSuspicion:F0}%\n" +
            $"Visible: {visibleBodyParts}/4 parts\n" +
            $"Status: {(ShouldChase ? "CHASE" : IsAlert ? "ALERT" : "PATROL")}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = barColor },
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );
    }
#endif
}