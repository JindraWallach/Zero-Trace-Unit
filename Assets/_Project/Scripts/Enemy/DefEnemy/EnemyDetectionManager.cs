using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BATCH DETECTION MANAGER - optimalizace pro multiple enemies.
/// Namísto 10 enemies checkujících každý frame, checkuje všechny v batchích.
/// UPDATED: Používá EnemyMultiPointVision místo EnemyVisionDetector.
/// </summary>
public class EnemyDetectionManager : MonoBehaviour
{
    private static EnemyDetectionManager instance;
    public static EnemyDetectionManager Instance => instance;

    [Header("Batch Detection Settings")]
    [Tooltip("How many enemies to check per frame (performance optimization)")]
    [SerializeField] private int enemiesPerBatch = 3;

    [Tooltip("Minimum time between full detection cycles (seconds)")]
    [SerializeField] private float minCycleTime = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Registered detectors
    private List<EnemyMultiPointVision> registeredVisionSystems = new List<EnemyMultiPointVision>();
    private Dictionary<EnemyMultiPointVision, float> lastCheckTimes = new Dictionary<EnemyMultiPointVision, float>();

    // Batch processing
    private int currentBatchIndex = 0;
    private Coroutine batchCoroutine;
    private bool isProcessing = false;

    private void Awake()
    {
        // Singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugMode)
            Debug.Log("[EnemyDetectionManager] Initialized", this);
    }

    private void OnEnable()
    {
        StartBatchProcessing();
    }

    private void OnDisable()
    {
        StopBatchProcessing();
    }

    // === REGISTRATION ===

    public void RegisterDetector(EnemyMultiPointVision visionSystem)
    {
        if (visionSystem == null || registeredVisionSystems.Contains(visionSystem))
            return;

        registeredVisionSystems.Add(visionSystem);
        lastCheckTimes[visionSystem] = 0f;

        if (debugMode)
            Debug.Log($"[EnemyDetectionManager] Registered {visionSystem.gameObject.name}. Total: {registeredVisionSystems.Count}", this);
    }

    public void UnregisterDetector(EnemyMultiPointVision visionSystem)
    {
        if (visionSystem == null)
            return;

        registeredVisionSystems.Remove(visionSystem);
        lastCheckTimes.Remove(visionSystem);

        if (debugMode)
            Debug.Log($"[EnemyDetectionManager] Unregistered {visionSystem.gameObject.name}. Total: {registeredVisionSystems.Count}", this);
    }

    // === BATCH PROCESSING ===

    private void StartBatchProcessing()
    {
        StopBatchProcessing();
        batchCoroutine = StartCoroutine(BatchDetectionCoroutine());

        if (debugMode)
            Debug.Log($"[EnemyDetectionManager] Started batch processing ({enemiesPerBatch} per frame)", this);
    }

    private void StopBatchProcessing()
    {
        if (batchCoroutine != null)
        {
            StopCoroutine(batchCoroutine);
            batchCoroutine = null;
        }

        isProcessing = false;
    }

    private IEnumerator BatchDetectionCoroutine()
    {
        isProcessing = true;
        var waitFrame = new WaitForEndOfFrame();

        while (isProcessing)
        {
            // Clean up null references
            registeredVisionSystems.RemoveAll(d => d == null);

            if (registeredVisionSystems.Count == 0)
            {
                yield return waitFrame;
                continue;
            }

            // Process batch
            int processed = 0;
            int batchSize = Mathf.Min(enemiesPerBatch, registeredVisionSystems.Count);

            for (int i = 0; i < batchSize; i++)
            {
                // Circular batch processing
                int index = (currentBatchIndex + i) % registeredVisionSystems.Count;
                EnemyMultiPointVision visionSystem = registeredVisionSystems[index];

                if (visionSystem != null && visionSystem.enabled)
                {
                    // Check if enough time passed since last check
                    float timeSinceLastCheck = Time.time - lastCheckTimes[visionSystem];

                    // Note: EnemyMultiPointVision už má vlastní coroutine pro vision checks,
                    // takže tento manager je teď optional backup/koordinátor
                    // Nechám to tady pro budoucí rozšíření (např. noise detection)

                    lastCheckTimes[visionSystem] = Time.time;
                    processed++;
                }
            }

            // Move to next batch
            currentBatchIndex = (currentBatchIndex + batchSize) % Mathf.Max(1, registeredVisionSystems.Count);

            if (debugMode && processed > 0)
            {
                Debug.Log($"[EnemyDetectionManager] Processed {processed} vision systems. Total registered: {registeredVisionSystems.Count}", this);
            }

            yield return waitFrame;
        }
    }

    // === MANUAL CHECK (for immediate detection) ===

    /// <summary>
    /// Force immediate detection check for specific vision system.
    /// Useful for triggering checks outside normal batch cycle.
    /// </summary>
    public void ForceCheck(EnemyMultiPointVision visionSystem)
    {
        if (visionSystem == null || !visionSystem.enabled)
            return;

        // Note: EnemyMultiPointVision handles its own checks via coroutine
        // This is just for manual override if needed
        lastCheckTimes[visionSystem] = Time.time;

        if (debugMode)
            Debug.Log($"[EnemyDetectionManager] Forced check for {visionSystem.gameObject.name}", this);
    }

    // === DEBUG INFO ===

    public int RegisteredCount => registeredVisionSystems.Count;
    public bool IsProcessing => isProcessing;

    private void OnGUI()
    {
        if (!debugMode)
            return;

        GUILayout.BeginArea(new Rect(10, 150, 300, 200));
        GUILayout.Label($"<b>Enemy Detection Manager</b>", new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Label($"Registered: {registeredVisionSystems.Count}");
        GUILayout.Label($"Batch Size: {enemiesPerBatch}");
        GUILayout.Label($"Current Index: {currentBatchIndex}");
        GUILayout.Label($"Processing: {isProcessing}");

        // Show registered enemies
        GUILayout.Label("<b>Registered Enemies:</b>", new GUIStyle(GUI.skin.label) { richText = true });
        foreach (var vision in registeredVisionSystems)
        {
            if (vision != null)
            {
                float timeSince = Time.time - lastCheckTimes[vision];
                string status = vision.CanSeePlayer ? "<color=red>SEES PLAYER</color>" : "<color=green>PATROLLING</color>";
                GUILayout.Label($"• {vision.gameObject.name}: {status} ({vision.VisiblePoints}/4 parts) - {timeSince:F2}s ago",
                    new GUIStyle(GUI.skin.label) { richText = true, fontSize = 10 });
            }
        }

        GUILayout.EndArea();
    }
}