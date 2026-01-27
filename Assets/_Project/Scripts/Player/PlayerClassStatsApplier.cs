using UnityEngine;

/// <summary>
/// Applies player class stat multipliers to game systems.
/// SRP: Only applies stats, doesn't manage class selection.
/// Attach to game scene manager or call on game start.
/// </summary>
public class PlayerClassStatsApplier : MonoBehaviour
{
    [Header("Config References")]
    [SerializeField] private NoiseConfig noiseConfig;
    [SerializeField] private EnemyConfig enemyConfig;
    [SerializeField] private SuspicionConfig suspicionConfig;

    [Header("Runtime References (Auto-found)")]
    [SerializeField] private Synty.AnimationBaseLocomotion.Samples.SamplePlayerAnimationController playerMovement;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private PlayerClassConfig appliedClass;

    private void Start()
    {
        ApplyClassStats();
    }

    /// <summary>
    /// Apply selected class stats to all game systems.
    /// </summary>
    public void ApplyClassStats()
    {
        if (PlayerClassManager.Instance == null)
        {
            Debug.LogError("[PlayerClassStatsApplier] PlayerClassManager not found!");
            return;
        }

        appliedClass = PlayerClassManager.Instance.SelectedClass;

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
        ApplyMovementStats();
    }

    /// <summary>
    /// Apply noise multipliers to NoiseConfig.
    /// </summary>
    private void ApplyNoiseStats()
    {
        if (noiseConfig == null)
        {
            Debug.LogWarning("[PlayerClassStatsApplier] NoiseConfig not assigned!");
            return;
        }

        float multiplier = appliedClass.noiseRadiusMultiplier;

        noiseConfig.walkNoiseRadius *= multiplier;
        noiseConfig.runNoiseRadius *= multiplier;
        noiseConfig.minLandingRadius *= multiplier;
        noiseConfig.maxLandingRadius *= multiplier;
        noiseConfig.doorOpenRadius *= multiplier;
        noiseConfig.doorCloseRadius *= multiplier;
        noiseConfig.flashlightToggleRadius *= multiplier;

        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Noise multiplier: {multiplier:F2}x");
    }

    /// <summary>
    /// Apply detection range to EnemyConfig.
    /// </summary>
    private void ApplyEnemyStats()
    {
        if (enemyConfig == null)
        {
            Debug.LogWarning("[PlayerClassStatsApplier] EnemyConfig not assigned!");
            return;
        }

        float multiplier = appliedClass.detectionRangeMultiplier;

        enemyConfig.visionRange *= multiplier;

        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Detection range multiplier: {multiplier:F2}x");
    }

    /// <summary>
    /// Apply suspicion build rate to SuspicionConfig.
    /// </summary>
    private void ApplySuspicionStats()
    {
        if (suspicionConfig == null)
        {
            Debug.LogWarning("[PlayerClassStatsApplier] SuspicionConfig not assigned!");
            return;
        }

        float multiplier = appliedClass.suspicionBuildMultiplier;

        suspicionConfig.baseBuildRate *= multiplier;

        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Suspicion build multiplier: {multiplier:F2}x");
    }

    /// <summary>
    /// Apply movement speed to player controller.
    /// Finds player movement component at runtime.
    /// </summary>
    private void ApplyMovementStats()
    {
        if (PlayerClassManager.Instance == null || !PlayerClassManager.Instance.IsPlayerSpawned())
        {
            Debug.LogWarning("[PlayerClassStatsApplier] Player not spawned yet, skipping movement stats.");
            return;
        }

        GameObject player = PlayerClassManager.Instance.GetPlayer();
        if (player == null)
        {
            Debug.LogWarning("[PlayerClassStatsApplier] Player GameObject is null!");
            return;
        }

        playerMovement = player.GetComponent<Synty.AnimationBaseLocomotion.Samples.SamplePlayerAnimationController>();

        if (playerMovement == null)
        {
            Debug.LogWarning("[PlayerClassStatsApplier] SamplePlayerMovement not found on player!");
            return;
        }

        // Apply speed multipliers (assuming SamplePlayerMovement has public speed fields)
        // You'll need to adjust based on your actual movement script
        float speedMultiplier = appliedClass.movementSpeedMultiplier;
        float sprintMultiplier = appliedClass.sprintDurationMultiplier;

        // Example (adjust field names to match your script):
        // playerMovement.walkSpeed *= speedMultiplier;
        // playerMovement.runSpeed *= speedMultiplier;
        // playerMovement.sprintDuration *= sprintMultiplier;

        if (debugLog)
            Debug.Log($"[PlayerClassStatsApplier] Movement speed: {speedMultiplier:F2}x, Sprint: {sprintMultiplier:F2}x");
    }

    /// <summary>
    /// Get applied class (for debugging).
    /// </summary>
    public PlayerClassConfig GetAppliedClass()
    {
        return appliedClass;
    }
}