using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager pro batch zpracování enemy vision detekce.
/// Každý detektor běží ve vlastní coroutině s nastavitelným intervalem.
///
/// FIX 2 – Dynamic interval:
///   SetFastInterval(detector)  → přepne na FAST_INTERVAL (0.05s) pro jeden detektor.
///   RestoreInterval(detector)  → vrátí na normální interval z SuspicionConfig.
///   Voláno z EnemyChaseState.Enter / Exit – žádný overhead pro ostatní enemy.
///
/// SRP:  Správa detekčních coroutin. Nic jiného.
/// OOP:  EnemyMultiPointVision nemusí vědět nic o manageru – jen implementuje PerformDetectionCheck().
/// Perf: WaitForSeconds alokován jednou per detektor per interval-change (ne každý frame).
/// </summary>
public class EnemyDetectionManager : MonoBehaviour
{
    public static EnemyDetectionManager Instance { get; private set; }

    // ── FIX 2: fast interval pro ChaseState ───────────────────────────────────
    /// <summary>Interval použitý v ChaseState – 20 checků/sekundu.</summary>
    private const float FAST_INTERVAL = 0.05f;

    [Header("Settings")]
    [Tooltip("Global detection interval override (0 = use per-enemy config)")]
    [Range(0f, 1f)]
    [SerializeField] private float globalDetectionInterval = 0f;

    [Header("Debug")]
    [SerializeField] private int registeredDetectors;
    [SerializeField] private int checksPerSecond;
    [SerializeField] private bool showDebugStats;

    // ── Internal state ────────────────────────────────────────────────────────
    private readonly List<EnemyMultiPointVision> detectors = new();
    private readonly Dictionary<EnemyMultiPointVision, Coroutine> detectorCoroutines = new();
    private readonly Dictionary<EnemyMultiPointVision, float> detectorIntervals = new();

    private int checksThisSecond;
    private float statsTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!showDebugStats) return;
        statsTimer += Time.deltaTime;
        if (statsTimer >= 1f)
        {
            checksPerSecond = checksThisSecond;
            checksThisSecond = 0;
            statsTimer = 0f;
        }
    }

    // ── Registration ──────────────────────────────────────────────────────────

    public void RegisterDetector(EnemyMultiPointVision detector)
    {
        if (detector == null || detectors.Contains(detector)) return;

        detectors.Add(detector);
        registeredDetectors = detectors.Count;

        float interval = GetNormalInterval(detector);
        StartDetectorCoroutine(detector, interval);

        if (Application.isEditor)
            Debug.Log($"[DetectionManager] Registered: {detector.gameObject.name} interval:{interval}s", this);
    }

    public void UnregisterDetector(EnemyMultiPointVision detector)
    {
        if (detector == null || !detectors.Contains(detector)) return;

        detectors.Remove(detector);
        registeredDetectors = detectors.Count;
        StopDetectorCoroutine(detector);
        detectorIntervals.Remove(detector);
    }

    // ── FIX 2: Dynamic interval API ───────────────────────────────────────────

    /// <summary>
    /// Přepne detektor na FAST_INTERVAL (0.05s).
    /// Volat z EnemyChaseState.Enter().
    /// </summary>
    public void SetFastInterval(EnemyMultiPointVision detector)
    {
        if (detector == null || !detectors.Contains(detector)) return;

        // Přepiš jen pokud se interval skutečně mění – zbytečný restart coroutiny stojí
        float current = detectorIntervals.TryGetValue(detector, out float v) ? v : -1f;
        if (Mathf.Approximately(current, FAST_INTERVAL)) return;

        RestartDetectorCoroutine(detector, FAST_INTERVAL);

        if (Application.isEditor)
            Debug.Log($"[DetectionManager] {detector.gameObject.name}: FAST interval ({FAST_INTERVAL}s)", this);
    }

    /// <summary>
    /// Vrátí detektor na normální interval z SuspicionConfig.
    /// Volat z EnemyChaseState.Exit().
    /// </summary>
    public void RestoreInterval(EnemyMultiPointVision detector)
    {
        if (detector == null || !detectors.Contains(detector)) return;

        float normal = GetNormalInterval(detector);
        float current = detectorIntervals.TryGetValue(detector, out float v) ? v : -1f;
        if (Mathf.Approximately(current, normal)) return;

        RestartDetectorCoroutine(detector, normal);

        if (Application.isEditor)
            Debug.Log($"[DetectionManager] {detector.gameObject.name}: restored interval ({normal}s)", this);
    }

    // ── Force checks ─────────────────────────────────────────────────────────

    public void ForceDetectionCheck(EnemyMultiPointVision detector)
    {
        if (detector != null && detectors.Contains(detector))
            detector.PerformDetectionCheck();
    }

    public void ForceAllDetectionChecks()
    {
        foreach (var d in detectors) d?.PerformDetectionCheck();
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    public void PauseAllDetection()
    {
        foreach (var d in detectors) StopDetectorCoroutine(d);
    }

    public void ResumeAllDetection()
    {
        foreach (var d in detectors)
        {
            if (d == null) continue;
            float interval = detectorIntervals.TryGetValue(d, out float v) ? v : GetNormalInterval(d);
            StartDetectorCoroutine(d, interval);
        }
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public List<EnemyMultiPointVision> GetDetectorsSeeingPlayer()
    {
        var result = new List<EnemyMultiPointVision>();
        foreach (var d in detectors)
            if (d != null && d.CanSeePlayer) result.Add(d);
        return result;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private float GetNormalInterval(EnemyMultiPointVision detector)
    {
        if (globalDetectionInterval > 0f) return globalDetectionInterval;

        var machine = detector.GetComponent<EnemyStateMachine>();
        return machine != null
            ? machine.Config.suspicionConfig.visionCheckInterval
            : 0.2f;
    }

    private void StartDetectorCoroutine(EnemyMultiPointVision detector, float interval)
    {
        StopDetectorCoroutine(detector);

        Coroutine c = StartCoroutine(DetectionCoroutine(detector, interval));
        detectorCoroutines[detector] = c;
        detectorIntervals[detector] = interval;
    }

    private void StopDetectorCoroutine(EnemyMultiPointVision detector)
    {
        if (!detectorCoroutines.TryGetValue(detector, out Coroutine c)) return;
        if (c != null) StopCoroutine(c);
        detectorCoroutines.Remove(detector);
    }

    private void RestartDetectorCoroutine(EnemyMultiPointVision detector, float interval)
    {
        StartDetectorCoroutine(detector, interval);
    }

    private IEnumerator DetectionCoroutine(EnemyMultiPointVision detector, float interval)
    {
        // Stagger initial check – rozloží zátěž po framech
        yield return new WaitForSeconds(Random.Range(0f, interval));

        var wait = new WaitForSeconds(interval);

        while (detector != null)
        {
            detector.PerformDetectionCheck();

            if (showDebugStats) checksThisSecond++;

            yield return wait;
        }
    }

    private void OnDestroy()
    {
        detectors.Clear();
        detectorCoroutines.Clear();
        detectorIntervals.Clear();
    }
}