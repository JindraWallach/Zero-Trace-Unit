using UnityEngine;

/// <summary>
/// Enemy hearing detection component.
/// Listens to NoiseSystem events and reacts if noise is in range.
/// Attach to enemy GameObjects with EnemyStateMachine.
/// </summary>
[RequireComponent(typeof(EnemyStateMachine))]
public class EnemyHearingDetector : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Multiplier for hearing range (1.0 = exact noise radius)")]
    [Range(0.5f, 2f)]
    [SerializeField] private float hearingMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private Vector3 lastHeardNoisePosition;
    [SerializeField] private float lastHeardNoiseTime;

    private EnemyStateMachine machine;

    private void Awake()
    {
        machine = GetComponent<EnemyStateMachine>();

        if (machine == null)
        {
            Debug.LogError($"[EnemyHearingDetector] {name} missing EnemyStateMachine!", this);
            enabled = false;
        }
    }

    private void Start()
    {
        // Subscribe to noise events
        if (NoiseSystem.Instance != null)
        {
            NoiseSystem.Instance.OnNoiseMade += OnNoiseHeard;
        }
        else
        {
            Debug.LogWarning($"[EnemyHearingDetector] {name} - NoiseSystem not found!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe
        if (NoiseSystem.Instance != null)
        {
            NoiseSystem.Instance.OnNoiseMade -= OnNoiseHeard;
        }
    }

    /// <summary>
    /// Called when any noise is emitted in the scene.
    /// Checks if noise is within hearing range.
    /// </summary>
    private void OnNoiseHeard(Vector3 noisePosition, float noiseRadius, NoiseType noiseType)
    {
        // Calculate distance to noise
        float distance = Vector3.Distance(transform.position, noisePosition);

        // Apply hearing multiplier
        float effectiveRadius = noiseRadius * hearingMultiplier;

        // Check if within range
        if (distance <= effectiveRadius)
        {
            lastHeardNoisePosition = noisePosition;
            lastHeardNoiseTime = Time.time;

            if (showDebugLogs)
            {
                Debug.Log($"[EnemyHearingDetector] {name} heard {noiseType} at distance {distance:F1}m " +
                         $"(max: {effectiveRadius:F1}m)", this);
            }

            // Notify current state
            machine.CurrentState?.OnNoiseHeard(noisePosition);
        }
    }

    // === DEBUG GIZMOS ===

    private void OnDrawGizmosSelected()
    {
        // Draw last heard noise position
        if (lastHeardNoiseTime > 0f && Time.time - lastHeardNoiseTime < 5f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastHeardNoisePosition, 0.5f);
            Gizmos.DrawLine(transform.position, lastHeardNoisePosition);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                lastHeardNoisePosition + Vector3.up * 0.5f,
                $"Last heard\n{Time.time - lastHeardNoiseTime:F1}s ago",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.yellow },
                    fontSize = 10
                }
            );
#endif
        }
    }
}