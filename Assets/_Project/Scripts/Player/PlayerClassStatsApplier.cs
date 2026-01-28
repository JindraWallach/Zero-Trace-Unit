using UnityEngine;

public class PlayerClassStatsApplier : MonoBehaviour
{
    [Header("Config References")]
    [SerializeField] private NoiseConfig noiseConfig;
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private SuspicionConfig suspicionConfig;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private PlayerClassConfig appliedClass;

    private void Start()
    {
        ApplyClassStats();
    }

    public void ApplyClassStats()
    {
        appliedClass = LoadSelectedClass();

        if (appliedClass == null)
        {
            Debug.LogWarning("[PlayerClassStatsApplier] No class selected, using default stats.");
            return;
        }

        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Applying stats for: {appliedClass.className}");

        ApplyNoiseStats();
        ApplyEnemyStats();
        ApplySuspicionStats();
    }

    private PlayerClassConfig LoadSelectedClass()
    {
        // Load from PlayerPrefs (saved by PlayerClassSelector)
        if (!PlayerPrefs.HasKey("SelectedClassIndex"))
            return null;

        int classIndex = PlayerPrefs.GetInt("SelectedClassIndex");

        // You need to load the class config - implement based on your setup
        // Option A: Store class name and load via Resources
        // Option B: Have a reference to PlayerClassSelector's class list
        // For now, return null - implement based on your needs

        Debug.LogWarning("[PlayerClassStatsApplier] Class loading not implemented - add your class loading logic here!");
        return null;
    }

    private void ApplyNoiseStats()
    {
        if (noiseConfig == null) return;
        float multiplier = appliedClass.noiseRadiusMultiplier;
        noiseConfig.walkNoiseRadius *= multiplier;
        noiseConfig.runNoiseRadius *= multiplier;
        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Noise multiplier: {multiplier:F2}x");
    }

    private void ApplyEnemyStats()
    {
        if (enemyConfig == null) return;
        float multiplier = appliedClass.detectionRangeMultiplier;
        enemyConfig.visionRange *= multiplier;
        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Detection range: {multiplier:F2}x");
    }

    private void ApplySuspicionStats()
    {
        if (suspicionConfig == null) return;
        float multiplier = appliedClass.suspicionBuildMultiplier;
        suspicionConfig.baseBuildRate *= multiplier;
        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Suspicion build: {multiplier:F2}x");
    }
}