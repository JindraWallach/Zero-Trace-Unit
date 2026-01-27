using UnityEngine;

/// <summary>
/// ScriptableObject defining a player class.
/// Contains identity, visual data, and stat multipliers.
/// SRP: Pure data container, no logic.
/// Create via: Assets > Create > Zero Trace > Player Class
/// </summary>
[CreateAssetMenu(fileName = "PlayerClass", menuName = "Zero Trace/Player Class")]
public class PlayerClassConfig : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name of the class")]
    public string className = "Ghost";

    [Tooltip("Prefix shown above name (e.g. 'Tier I Operative')")]
    public string classPrefix = "Tier I Operative";

    [Tooltip("Description shown in selection UI")]
    [TextArea(3, 5)]
    public string description = "Master of silent infiltration. Reduced noise generation at the cost of movement speed.";

    [Header("Visual")]
    [Tooltip("Player prefab with specific skin/model")]
    public GameObject playerPrefab;

    [Tooltip("Icon displayed in UI")]
    public Sprite classIcon;

    [Tooltip("Primary UI color for this class")]
    public Color primaryColor = Color.white;

    [Header("Movement Stats")]
    [Tooltip("Movement speed multiplier (1.0 = normal)")]
    [Range(0.7f, 1.3f)]
    public float movementSpeedMultiplier = 1.0f;

    [Tooltip("Sprint duration multiplier (1.0 = normal)")]
    [Range(0.7f, 1.5f)]
    public float sprintDurationMultiplier = 1.0f;

    [Header("Stealth Stats")]
    [Tooltip("Noise radius multiplier (lower = stealthier)")]
    [Range(0.5f, 1.5f)]
    public float noiseRadiusMultiplier = 1.0f;

    [Tooltip("Enemy detection range multiplier (lower = harder to detect)")]
    [Range(0.8f, 1.2f)]
    public float detectionRangeMultiplier = 1.0f;

    [Tooltip("Suspicion build rate multiplier (lower = slower detection)")]
    [Range(0.7f, 1.3f)]
    public float suspicionBuildMultiplier = 1.0f;

    [Header("Hacking Stats")]
    [Tooltip("Puzzle time limit multiplier (higher = more time)")]
    [Range(0.7f, 1.5f)]
    public float hackTimeMultiplier = 1.0f;

    /// <summary>
    /// Get normalized stat value for UI bar (0-10 scale).
    /// 1.0 multiplier = 5 (middle), 0.5 = 0, 1.5 = 10
    /// </summary>
    public int GetNormalizedStat(StatType type)
    {
        float multiplier = type switch
        {
            StatType.Speed => movementSpeedMultiplier,
            StatType.Stealth => 2.0f - noiseRadiusMultiplier, // Invert (lower noise = higher stat)
            StatType.Hacking => hackTimeMultiplier,
            StatType.Detection => 2.0f - detectionRangeMultiplier, // Invert
            _ => 1.0f
        };

        return Mathf.RoundToInt((multiplier - 0.5f) * 10f);
    }

    /// <summary>
    /// Get stat display percentage (e.g. "-10%", "+40%")
    /// </summary>
    public string GetStatPercentage(StatType type)
    {
        float multiplier = type switch
        {
            StatType.Speed => movementSpeedMultiplier,
            StatType.Stealth => 2.0f - noiseRadiusMultiplier,
            StatType.Hacking => hackTimeMultiplier,
            StatType.Detection => 2.0f - detectionRangeMultiplier,
            _ => 1.0f
        };

        int percentage = Mathf.RoundToInt((multiplier - 1.0f) * 100f);
        return percentage > 0 ? $"+{percentage}%" : $"{percentage}%";
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(className))
            className = "Unnamed Class";

        if (playerPrefab == null)
            Debug.LogWarning($"[{className}] Player prefab not assigned!", this);
    }
}

/// <summary>
/// Stat types for UI display.
/// </summary>
public enum StatType
{
    Speed,
    Stealth,
    Hacking,
    Detection
}