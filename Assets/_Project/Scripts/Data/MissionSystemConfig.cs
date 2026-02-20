// Scripts/Data/MissionSystemConfig.cs
using UnityEngine;

/// <summary>
/// ScriptableObject pro konfiguraci ServerCore promptů a mission systému.
/// Single source of truth – všechny texty, barvy a ranges na jednom místě.
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

    // ── Server Hack – Ranges ───────────────────────────────────────────────

    [Header("Server Hack – Ranges")]
    [Tooltip("Vzdálenost, ve které se zobrazuje jakýkoliv prompt (Normal i Hack mode)")]
    [Range(5f, 30f)]
    public float interactionRange = 6f;

    [Tooltip("Vzdálenost potřebná pro samotný hack (Hack mode)")]
    [Range(5f, 30f)]
    public float serverHackRange = 15f;

    // ── Server Hack – Prompt Texts ─────────────────────────────────────────

    [Header("Server Hack – Prompt Texts")]
    [Tooltip("Text v Normal modu – hráč nemá přístup (červená)")]
    public string lockedText = "Authorization Required";

    [Tooltip("Text v Hack modu – hráč je mimo dosah (červená)")]
    public string outOfRangeText = "OUT OF RANGE";

    [Tooltip("Text akce pro hack – zobrazí se jako [E] HACK (žlutá)")]
    public string hackText = "HACK";

    [Tooltip("Text po úspěšném hacknutí (šedá)")]
    public string hackedText = "HACKED";

    [Tooltip("Klávesa zobrazená v promptu – např. E")]
    public string interactKey = "E";

    // ── Identifikace ───────────────────────────────────────────────────────

    [Header("Identifikace")]
    [Tooltip("Tag hráče pro EscapeZone trigger")]
    public string playerTag = "Player";

    [Tooltip("ID serveru pro HackManager registraci")]
    public string serverTargetID = "ServerCore";

    // ── Debug ──────────────────────────────────────────────────────────────

    [Header("Debug")]
    public bool debugLog = true;

    // ── Validation ─────────────────────────────────────────────────────────

    private void OnValidate()
    {
        interactionRange = Mathf.Max(0.5f, interactionRange);
        serverHackRange = Mathf.Max(0.5f, serverHackRange);

        if (serverHackRange < interactionRange)
            Debug.LogWarning("[MissionSystemConfig] serverHackRange je menší než interactionRange – hráč uvidí prompt, ale bude vždy OUT OF RANGE.");

        if (string.IsNullOrWhiteSpace(interactKey))
            interactKey = "E";
    }
}