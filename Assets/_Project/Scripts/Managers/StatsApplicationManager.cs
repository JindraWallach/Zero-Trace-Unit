using UnityEngine;

/// <summary>
/// Prevents multiplier stacking by tracking if stats have been applied this session.
/// Works with existing PlayerPersistence system - doesn't replace it.
/// Single Responsibility: Ensure one-time stats application per game session.
/// </summary>
public class StatsApplicationManager : MonoBehaviour
{
    private static StatsApplicationManager instance;
    private static bool statsAppliedThisSession = false;

    [Header("References")]
    [SerializeField] private BaseStats baseStats;
    [SerializeField] private EnemyConfig enemyConfig;

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private string lastAppliedClass = "None";

    private void Awake()
    {
        // Singleton with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugMode)
            Debug.Log($"[StatsApplicationManager] Initialized. Stats applied: {statsAppliedThisSession}");
    }

    /// <summary>
    /// Apply class multipliers to configs - ONLY if not already applied this session.
    /// Called by PlayerPersistence or directly from menu.
    /// </summary>
    public void ApplyClassStats(PlayerClassConfig classConfig)
    {
        if (classConfig == null)
        {
            Debug.LogError("[StatsApplicationManager] Cannot apply null class config!");
            return;
        }

        if (statsAppliedThisSession)
        {
            if (debugMode)
                Debug.LogWarning($"[StatsApplicationManager] Stats already applied this session (class: {lastAppliedClass}). Ignoring duplicate call for: {classConfig.className}");
            return;
        }

        // Validate references
        if (baseStats == null || enemyConfig == null)
        {
            Debug.LogError("[StatsApplicationManager] Missing references! Assign BaseStats and EnemyConfig in inspector.");
            return;
        }
        Debug.LogWarning($"[StatsApplicationManager] Applying stats for class: {classConfig.className} (Detection: {classConfig.detectionStat}%, Speed: {classConfig.speedStat}%, Stealth: {classConfig.stealthStat}%)");

        // Convert percentage stats to multipliers (100% = 1.0x)
        float enemyVisionMult = classConfig.detectionStat / 100f;
        float enemySpeedMult = classConfig.speedStat / 100f; // Apply to enemy speeds (inverse relation)
        float playerSpeedMult = classConfig.speedStat / 100f;
        float stealthMult = classConfig.stealthStat / 100f;

        // Apply to EnemyConfig
        enemyConfig.visionRange = baseStats.originalVisionRange * enemyVisionMult;
        enemyConfig.patrolSpeed = baseStats.originalPatrolSpeed * enemySpeedMult;
        enemyConfig.chaseSpeed = baseStats.originalChaseSpeed * enemySpeedMult;
        enemyConfig.searchSpeed = baseStats.originalSearchSpeed * enemySpeedMult;

        // Mark as applied
        statsAppliedThisSession = true;
        lastAppliedClass = classConfig.className;

        if (debugMode)
        {
            Debug.Log($"[StatsApplicationManager] ✓ Applied {classConfig.className} stats:\n" +
                     $"  Detection: {classConfig.detectionStat}% → Enemy Vision: {baseStats.originalVisionRange} * {enemyVisionMult:F2} = {enemyConfig.visionRange:F1}m\n" +
                     $"  Speed: {classConfig.speedStat}% → Enemy Patrol: {baseStats.originalPatrolSpeed} * {enemySpeedMult:F2} = {enemyConfig.patrolSpeed:F2}\n" +
                     $"  Speed: {classConfig.speedStat}% → Enemy Chase: {baseStats.originalChaseSpeed} * {enemySpeedMult:F2} = {enemyConfig.chaseSpeed:F2}\n" +
                     $"  Stealth: {classConfig.stealthStat}% (visual stats only)");
        }
    }

    /// <summary>
    /// Check if stats have been applied this session.
    /// </summary>
    public bool HasAppliedStats() => statsAppliedThisSession;

    /// <summary>
    /// Get last applied class name.
    /// </summary>
    public string GetLastAppliedClass() => lastAppliedClass;

    /// <summary>
    /// Reset stats to base values (for quitting game or testing).
    /// DO NOT call on scene restart - only on application quit or new game session.
    /// </summary>
    public void ResetToBaseStats()
    {
        if (baseStats == null || enemyConfig == null)
        {
            Debug.LogWarning("[StatsApplicationManager] Cannot reset - missing references!");
            return;
        }

        enemyConfig.visionRange = baseStats.originalVisionRange;
        enemyConfig.patrolSpeed = baseStats.originalPatrolSpeed;
        enemyConfig.chaseSpeed = baseStats.originalChaseSpeed;
        enemyConfig.searchSpeed = baseStats.originalSearchSpeed;

        statsAppliedThisSession = false;
        lastAppliedClass = "None";

        if (debugMode)
            Debug.Log("[StatsApplicationManager] Stats reset to base values.");
    }

    private void OnApplicationQuit()
    {
        // Reset for next game session
        ResetToBaseStats();
    }

    // Static accessor
    public static StatsApplicationManager Instance => instance;

    /// <summary>
    /// Force reset for testing purposes (e.g., returning to main menu).
    /// Call this when starting a completely new game session.
    /// </summary>
    public void ForceResetForNewSession()
    {
        ResetToBaseStats();
        Debug.Log("[StatsApplicationManager] Forced reset for new session.");
    }
}