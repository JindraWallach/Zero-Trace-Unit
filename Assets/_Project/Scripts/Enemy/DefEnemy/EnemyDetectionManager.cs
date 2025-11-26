using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for batch processing enemy vision detection.
/// Instead of each enemy checking every frame (expensive), we batch checks:
/// - 6 enemies × 60 fps = 360 checks/sec (BAD)
/// - 6 enemies ÷ 0.1s interval = 60 checks/sec (GOOD - 6x optimization)
/// 
/// Performance:
/// - Distributes checks across frames (load balancing)
/// - Configurable interval per enemy (via EnemyConfig)
/// - No memory allocations (cached lists)
/// </summary>
public class EnemyDetectionManager : MonoBehaviour
{
    public static EnemyDetectionManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Global detection interval override (0 = use per-enemy config)")]
    [Range(0f, 1f)]
    [SerializeField] private float globalDetectionInterval = 0f;

    [Header("Debug")]
    [SerializeField] private int registeredDetectors = 0;
    [SerializeField] private int checksPerSecond = 0;
    [SerializeField] private bool showDebugStats = false;

    // Registered detectors
    private readonly List<EnemyVisionDetector> detectors = new List<EnemyVisionDetector>();
    private readonly Dictionary<EnemyVisionDetector, Coroutine> detectorCoroutines = new Dictionary<EnemyVisionDetector, Coroutine>();

    // Performance tracking
    private int checksThisSecond;
    private float statsTimer;

    private void Awake()
    {
        // Singleton pattern (no DontDestroyOnLoad - per scene)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Update debug stats
        if (showDebugStats)
        {
            statsTimer += Time.deltaTime;
            if (statsTimer >= 1f)
            {
                checksPerSecond = checksThisSecond;
                checksThisSecond = 0;
                statsTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Register a vision detector for batch processing.
    /// </summary>
    public void RegisterDetector(EnemyVisionDetector detector)
    {
        if (detector == null || detectors.Contains(detector))
            return;

        detectors.Add(detector);
        registeredDetectors = detectors.Count;

        // Start detection coroutine for this detector
        float interval = globalDetectionInterval > 0
            ? globalDetectionInterval
            : detector.GetComponent<EnemyStateMachine>().Config.visionCheckInterval;

        Coroutine coroutine = StartCoroutine(DetectionCoroutine(detector, interval));
        detectorCoroutines[detector] = coroutine;

        Debug.Log($"[DetectionManager] Registered detector: {detector.gameObject.name}, interval: {interval}s", this);
    }

    /// <summary>
    /// Unregister a vision detector.
    /// </summary>
    public void UnregisterDetector(EnemyVisionDetector detector)
    {
        if (detector == null || !detectors.Contains(detector))
            return;

        detectors.Remove(detector);
        registeredDetectors = detectors.Count;

        // Stop coroutine
        if (detectorCoroutines.TryGetValue(detector, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            detectorCoroutines.Remove(detector);
        }

        Debug.Log($"[DetectionManager] Unregistered detector: {detector.gameObject.name}", this);
    }

    /// <summary>
    /// Coroutine that runs detection checks at specified interval.
    /// </summary>
    private IEnumerator DetectionCoroutine(EnemyVisionDetector detector, float interval)
    {
        // Stagger initial checks to distribute load
        yield return new WaitForSeconds(Random.Range(0f, interval));

        WaitForSeconds wait = new WaitForSeconds(interval);

        while (detector != null)
        {
            // Perform detection check
            detector.PerformDetectionCheck();

            // Track stats
            if (showDebugStats)
                checksThisSecond++;

            yield return wait;
        }
    }

    /// <summary>
    /// Force immediate detection check for specific detector (emergency check).
    /// </summary>
    public void ForceDetectionCheck(EnemyVisionDetector detector)
    {
        if (detector == null || !detectors.Contains(detector))
            return;

        detector.PerformDetectionCheck();
    }

    /// <summary>
    /// Force detection check for all registered detectors.
    /// </summary>
    public void ForceAllDetectionChecks()
    {
        foreach (var detector in detectors)
        {
            if (detector != null)
                detector.PerformDetectionCheck();
        }
    }

    /// <summary>
    /// Pause all detection (e.g., during cutscenes/puzzles).
    /// </summary>
    public void PauseAllDetection()
    {
        foreach (var kvp in detectorCoroutines)
        {
            if (kvp.Value != null)
                StopCoroutine(kvp.Value);
        }
    }

    /// <summary>
    /// Resume all detection after pause.
    /// </summary>
    public void ResumeAllDetection()
    {
        foreach (var detector in detectors)
        {
            if (detector == null) continue;

            float interval = globalDetectionInterval > 0
                ? globalDetectionInterval
                : detector.GetComponent<EnemyStateMachine>().Config.visionCheckInterval;

            Coroutine coroutine = StartCoroutine(DetectionCoroutine(detector, interval));
            detectorCoroutines[detector] = coroutine;
        }
    }

    /// <summary>
    /// Get all detectors that can see player right now.
    /// </summary>
    public List<EnemyVisionDetector> GetDetectorsSeingPlayer()
    {
        List<EnemyVisionDetector> result = new List<EnemyVisionDetector>();

        foreach (var detector in detectors)
        {
            if (detector != null && detector.CanSeePlayerNow)
                result.Add(detector);
        }

        return result;
    }

    private void OnDestroy()
    {
        // Cleanup
        detectors.Clear();
        detectorCoroutines.Clear();
    }
}