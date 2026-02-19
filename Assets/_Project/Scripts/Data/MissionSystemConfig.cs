// Scripts/Data/MissionSystemConfig.cs
using UnityEngine;

/// <summary>
/// ScriptableObject pro konfiguraci mission systému.
/// Nastavitelné hodnoty – žádný hardcode v scriptech.
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

    [Header("Server Hack")]
    [Tooltip("Tag hráče pro EscapeZone trigger")]
    public string playerTag = "Player";

    [Tooltip("ID serveru pro HackManager registraci")]
    public string serverTargetID = "ServerCore";

    [Header("Debug")]
    public bool debugLog = true;
}