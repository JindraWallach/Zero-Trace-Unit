using UnityEngine;

[CreateAssetMenu(fileName = "PlayerClass", menuName = "Zero Trace/Player Class")]
public class PlayerClassConfig : ScriptableObject
{
    [Header("Identity")]
    public string className = "Ghost";
    public string classPrefix = "Tier I Operative";
    [TextArea(3, 5)]
    public string description = "Master of silent infiltration.";

    [Header("Visual - Skin Mesh")]
    public Mesh characterMesh;
    public Material characterMaterial;
    public Sprite classIcon;
    public Sprite classPreviewSprite;
    public Color primaryColor = Color.white;

    [Header("Movement Stats")]
    [Range(0.7f, 1.3f)] public float movementSpeedMultiplier = 1.0f;
    [Range(0.7f, 1.5f)] public float sprintDurationMultiplier = 1.0f;

    [Header("Stealth Stats")]
    [Range(0.5f, 1.5f)] public float noiseRadiusMultiplier = 1.0f;
    [Range(0.8f, 1.2f)] public float detectionRangeMultiplier = 1.0f;
    [Range(0.7f, 1.3f)] public float suspicionBuildMultiplier = 1.0f;

    [Header("Hacking Stats")]
    [Range(0.7f, 1.5f)] public float hackTimeMultiplier = 1.0f;

    public int GetNormalizedStat(StatType type)
    {
        float multiplier = type switch
        {
            StatType.Speed => movementSpeedMultiplier,
            StatType.Stealth => 2.0f - noiseRadiusMultiplier,
            StatType.Hacking => hackTimeMultiplier,
            StatType.Detection => 2.0f - detectionRangeMultiplier,
            _ => 1.0f
        };
        return Mathf.RoundToInt((multiplier - 0.5f) * 10f);
    }

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
}

public enum StatType { Speed, Stealth, Hacking, Detection }