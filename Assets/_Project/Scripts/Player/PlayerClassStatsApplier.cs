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
        // Follow same approach as PlayerPersistence: load class asset from Resources by saved name.
        if (!PlayerPrefs.HasKey("SelectedClassName"))
        {
            if (debugLog)
                Debug.Log("[PlayerClassStatsApplier] No class selected in PlayerPrefs (SelectedClassName missing)");
            return null;
        }

        string classAssetName = PlayerPrefs.GetString("SelectedClassName");
        if (string.IsNullOrEmpty(classAssetName))
        {
            if (debugLog)
                Debug.LogWarning("[PlayerClassStatsApplier] SelectedClassName is empty in PlayerPrefs");
            return null;
        }

        // Expect PlayerClassConfig assets to be located in Resources/PlayerClasses/
        PlayerClassConfig loaded = Resources.Load<PlayerClassConfig>($"PlayerClasses/{classAssetName}");
        if (loaded == null)
            Debug.LogWarning($"[PlayerClassStatsApplier] Could not load class '{classAssetName}' from Resources/PlayerClasses/");

        return loaded;
    }

    private void ApplyNoiseStats()
    {
        if (noiseConfig == null || appliedClass == null) return;
        float multiplier = appliedClass.noiseRadiusMultiplier;
        noiseConfig.walkNoiseRadius *= multiplier;
        noiseConfig.runNoiseRadius *= multiplier;
        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Noise multiplier: {multiplier:F2}x");
    }

    private void ApplyEnemyStats()
    {
        if (enemyConfig == null || appliedClass == null) return;
        float multiplier = appliedClass.detectionRangeMultiplier;
        enemyConfig.visionRange *= multiplier;
        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Detection range: {multiplier:F2}x");
    }

    private void ApplySuspicionStats()
    {
        if (suspicionConfig == null || appliedClass == null) return;
        float multiplier = appliedClass.suspicionBuildMultiplier;
        suspicionConfig.baseBuildRate *= multiplier;
        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Suspicion build: {multiplier:F2}x");
    }
}