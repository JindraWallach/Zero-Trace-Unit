using UnityEngine;

/// <summary>
/// Configuration for player character classes.
/// Defines stats, appearance (multiple parts), and gameplay modifiers.
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

    [Header("Base Stats (0-10 scale)")]
    [Range(0, 10)] public int speedStat = 5;
    [Range(0, 10)] public int stealthStat = 5;
    [Range(0, 10)] public int hackingStat = 5;
    [Range(0, 10)] public int detectionStat = 5;

    [Header("Gameplay Modifiers")]
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
    }

    /// <summary>
    /// Get normalized stat value (0-10).
    /// </summary>
    public int GetNormalizedStat(StatType statType)
    {
        return statType switch
        {
            StatType.Speed => speedStat,
            StatType.Stealth => stealthStat,
            StatType.Hacking => hackingStat,
            StatType.Detection => detectionStat,
            _ => 5
        };
    }

    /// <summary>
    /// Get stat as percentage string (e.g. "80%").
    /// </summary>
    public string GetStatPercentage(StatType statType)
    {
        int value = GetNormalizedStat(statType);
        return $"{value * 10}%";
    }
}

public enum StatType
{
    Speed,
    Stealth,
    Hacking,
    Detection
}