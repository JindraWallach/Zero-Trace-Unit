using UnityEngine;

/// <summary>
/// Configuration for player character classes.
/// Defines stats (as percentages), appearance (multiple parts), and gameplay modifiers.
/// UPDATED: Stats now use percentage system (0-200%, default 100%)
/// Create via: Assets > Create > Zero Trace > Player Class Config
/// </summary>
[CreateAssetMenu(fileName = "PlayerClassConfig", menuName = "Zero Trace/Player Class Config")]
public class PlayerClassConfig : ScriptableObject
{
    [Header("Class Identity")]
    [Tooltip("Display name (e.g. 'Ghost')")]
    public string className = "New Class";

    [Tooltip("Short prefix (e.g. 'GHO')")]
    public string classPrefix = "CLS";

    [Tooltip("Description shown in UI")]
    [TextArea(3, 6)]
    public string description = "Class description here.";

    [Header("Visual Identity")]
    [Tooltip("Icon for UI")]
    public Sprite classIcon;

    [Tooltip("Preview image for selection screen (optional)")]
    public Sprite classPreviewSprite;

    [Tooltip("Primary color for UI theming")]
    public Color primaryColor = Color.white;

    [Header("Character Appearance")]
    [Tooltip("All visual parts that define this class's appearance")]
    public CharacterPart[] characterParts;

    [Header("Base Stats (Percentage: 0-200%, Default 100%)")]
    [Tooltip("Player movement speed: 100% = normal, 80% = slower, 120% = faster")]
    [Range(0, 200)] public int speedStat = 100;

    [Tooltip("Stealth effectiveness: 100% = normal, 80% = harder to detect, 120% = easier to detect")]
    [Range(0, 200)] public int stealthStat = 100;

    [Tooltip("Hacking speed/effectiveness: 100% = normal, 120% = faster hacks")]
    [Range(0, 200)] public int hackingStat = 100;

    [Tooltip("Enemy detection range: 100% = normal, 80% = enemies see less, 120% = enemies see more")]
    [Range(0, 200)] public int detectionStat = 100;

    [Header("Gameplay Modifiers (Legacy - use stats above instead)")]
    [Tooltip("Movement speed multiplier (1.0 = normal, 1.2 = 20% faster)")]
    [Range(0.5f, 2.0f)]
    public float movementSpeedMultiplier = 1.0f;

    [Tooltip("Noise radius multiplier (0.8 = 20% quieter, 1.2 = 20% louder)")]
    [Range(0.5f, 2.0f)]
    public float noiseRadiusMultiplier = 1.0f;

    [Tooltip("Enemy detection range multiplier (0.8 = harder to spot, 1.2 = easier to spot)")]
    [Range(0.5f, 2.0f)]
    public float detectionRangeMultiplier = 1.0f;

    [Tooltip("Suspicion build rate multiplier (0.7 = slower suspicion, 1.3 = faster)")]
    [Range(0.5f, 2.0f)]
    public float suspicionBuildMultiplier = 1.0f;

    private void OnValidate()
    {
        // Ensure class has at least one part
        if (characterParts == null || characterParts.Length == 0)
        {
            Debug.LogWarning($"[PlayerClassConfig] {className} has no character parts defined!");
        }

        // Check for duplicate part types
        if (characterParts != null)
        {
            for (int i = 0; i < characterParts.Length; i++)
            {
                for (int j = i + 1; j < characterParts.Length; j++)
                {
                    if (characterParts[i].partType == characterParts[j].partType)
                    {
                        Debug.LogWarning($"[PlayerClassConfig] {className} has duplicate {characterParts[i].partType} parts!");
                    }
                }
            }
        }

        // Auto-sync legacy multipliers with percentage stats
        movementSpeedMultiplier = speedStat / 100f;
        detectionRangeMultiplier = detectionStat / 100f;
    }

    /// <summary>
    /// Get stat value as percentage (0-200).
    /// </summary>
    public int GetStatPercentage(StatType statType)
    {
        return statType switch
        {
            StatType.Speed => speedStat,
            StatType.Stealth => stealthStat,
            StatType.Hacking => hackingStat,
            StatType.Detection => detectionStat,
            _ => 100
        };
    }

    /// <summary>
    /// Get stat value as multiplier (0.0-2.0, where 1.0 = 100%).
    /// </summary>
    public float GetStatMultiplier(StatType statType)
    {
        return GetStatPercentage(statType) / 100f;
    }

    /// <summary>
    /// Get stat as percentage string (e.g. "80%", "120%").
    /// </summary>
    public string GetStatPercentageString(StatType statType)
    {
        return $"{GetStatPercentage(statType)}%";
    }

    /// <summary>
    /// Get normalized stat value for UI bars (0-1 range).
    /// 100% = 0.5 (middle), 0% = 0, 200% = 1.0
    /// </summary>
    public float GetNormalizedStatForUI(StatType statType)
    {
        return GetStatPercentage(statType) / 200f;
    }

    /// <summary>
    /// DEPRECATED: Use GetStatPercentage() instead.
    /// Kept for backward compatibility with old UI code.
    /// </summary>
    [System.Obsolete("Use GetStatPercentage() instead. This returns value/10 for old 0-10 system.")]
    public int GetNormalizedStat(StatType statType)
    {
        // Convert 0-200% to old 0-10 scale for backward compatibility
        return GetStatPercentage(statType) / 20;
    }
}

public enum StatType
{
    Speed,
    Stealth,
    Hacking,
    Detection
}