// Scripts/Data/MissionSystemConfig.cs
using UnityEngine;

/// <summary>
/// ScriptableObject pro konfiguraci mission systému.
/// Create via: Assets > Create > Zero Trace > Mission System Config
/// </summary>
[CreateAssetMenu(fileName = "MissionSystemConfig", menuName = "Zero Trace/Mission System Config")]
public class MissionSystemConfig : ScriptableObject
{
    [Header("Mission Complete UI")]
    [Tooltip("Text zobrazený po dokončení mise")]
    public string missionCompleteText = "MISSION COMPLETE";

    [Tooltip("Barva textu MISSION COMPLETE")]
    public Color missionCompleteColor = Color.white;

    [Header("Server Hack – Prompt")]
    [Tooltip("Hack range terminálu (stejné jako hackRange u dveří)")]
    [Range(5f, 30f)]
    public float serverHackRange = 15f;

    [Tooltip("Klávesa zobrazená v promptu – např. E")]
    public string interactKey = "E";

    [Header("Identifikace")]
    [Tooltip("Tag hráče pro EscapeZone trigger")]
    public string playerTag = "Player";

    [Tooltip("ID serveru pro HackManager registraci")]
    public string serverTargetID = "ServerCore";

    [Header("Debug")]
    public bool debugLog = true;
}