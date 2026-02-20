// Scripts/Mission/ServerCoreInteractionMode.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// Prompt controller pro ServerCore.
/// Stejný pattern jako DoorInteractionMode:
///   – event-driven (OnModeChanged) → okamžitý refresh bez delay
///   – coroutine polling vzdálenosti (5x/s) pouze když je hráč v range
///   – žádný Update(), žádné každosnímkové volání
///
/// Stavy promptu:
///   Normal mode   → [lockedText] červená   (Authorization Required, …)
///   Hack mode, out of range → [outOfRangeText] červená
///   Hack mode, in range    → [E] HACK žlutá
///   Hacked                 → HACKED šedá
///
/// SRP: Pouze zodpovídá za zobrazení promptu.
///      Hackování deleguje na ServerCore.RequestHack().
/// </summary>
[DisallowMultipleComponent]
public class ServerCoreInteractionMode : MonoBehaviour
{
    [Header("References")]
    [Tooltip("UIPromptController přiřazený na terminálu")]
    [SerializeField] private UIPromptController prompt;

    [Tooltip("Alarm systém – triggeruje se při neúspěšném hacku (optional, auto-find)")]
    [SerializeField] private SecurityAlarmSystem alarmSystem;

    [Header("Config")]
    [SerializeField] private MissionSystemConfig config;

    [Header("Update Settings")]
    [Tooltip("Jak často se polluje vzdálenost (s) – doporučeno 0.2")]
    [SerializeField] private float updateInterval = 0.2f;

    // ── Barvy (stejná paleta jako DoorInteractionConfig) ──────────────────
    private static readonly Color ColorHack = new Color(1f, 0.8f, 0f);          // žlutá
    private static readonly Color ColorOutOfRange = new Color(1f, 0.2f, 0.2f);        // červená
    private static readonly Color ColorLocked = new Color(1f, 0.2f, 0.2f);        // červená
    private static readonly Color ColorHacked = new Color(0.5f, 0.5f, 0.5f);      // šedá

    // ── Runtime state ──────────────────────────────────────────────────────
    private Transform _player;
    private bool _playerInRange;
    private bool _hacked;
    private Coroutine _updateCoroutine;

    // ── Cached WaitForSeconds (zero allocation) ────────────────────────────
    private WaitForSeconds _wait;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _wait = new WaitForSeconds(updateInterval);
    }

    private void OnEnable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;
    }

    private void OnDisable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;

        StopUpdating();
    }

    // ── Public API (volá ServerCore) ───────────────────────────────────────

    /// <summary>
    /// Volá PlayerInteractor přes ServerCore.ShowPromptForPlayer / HidePromptForPlayer.
    /// </summary>
    public void SetPlayerInRange(Transform player, bool inRange)
    {
        _player = player;
        _playerInRange = inRange;

        if (inRange)
            StartUpdating();
        else
            StopUpdating();
    }

    /// <summary>
    /// Volá ServerCore po úspěšném hacknutí – zobrazí finální "HACKED" stav.
    /// </summary>
    public void OnHackSuccess()
    {
        _hacked = true;
        prompt?.Show("HACKED", ColorHacked);
    }

    /// <summary>
    /// Spustí hack nebo fyzickou interakci podle aktuálního stavu.
    /// Voláno z ServerCore.Interact().
    /// </summary>
    public void ExecuteInteraction()
    {
        if (_hacked) return;
        if (!_playerInRange) return;

        if (PlayerModeController.Instance.CurrentMode != PlayerMode.Hack) return;

        float hackRange = config != null ? config.serverHackRange : 15f;
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > hackRange) return;

        // Delegate hack to ServerCore (which owns RequestHack)
        var serverCore = GetComponent<ServerCore>();
        serverCore?.RequestHack(
            onSuccess: null,        // HandleHackSuccess voláno uvnitř ServerCore
            onFail: OnHackFail,
            onCancel: () => { if (config != null && config.debugLog) Debug.Log("[ServerCoreIM] Hack cancelled – no alarm."); }
        );
    }

    // ── Alarm ──────────────────────────────────────────────────────────────

    private void OnHackFail()
    {
        if (config != null && config.debugLog)
            Debug.Log("[ServerCoreIM] Hack failed – triggering alarm.");

        if (alarmSystem != null)
        {
            alarmSystem.TriggerAlarm(transform.position);
            return;
        }

        // Auto-find fallback (stejný pattern jako DoorInteractionMode)
        alarmSystem = FindFirstObjectByType<SecurityAlarmSystem>();
        if (alarmSystem != null)
            alarmSystem.TriggerAlarm(transform.position);
        else
            Debug.LogError("[ServerCoreIM] No SecurityAlarmSystem found in scene!", this);
    }

    // ── Event callbacks ────────────────────────────────────────────────────

    private void OnModeChanged(PlayerMode _)
    {
        // Okamžitý refresh bez čekání na interval
        if (_playerInRange)
            RefreshPrompt();
    }

    // ── Coroutine polling (pouze když je hráč v range) ─────────────────────

    private void StartUpdating()
    {
        StopUpdating();
        _updateCoroutine = StartCoroutine(UpdateCoroutine());
    }

    private void StopUpdating()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }

        prompt?.Hide();
    }

    private IEnumerator UpdateCoroutine()
    {
        while (_playerInRange && _player != null)
        {
            RefreshPrompt();
            yield return _wait;
        }
    }

    // ── Prompt logic ───────────────────────────────────────────────────────

    private void RefreshPrompt()
    {
        if (prompt == null || _player == null) return;

        // Terminál byl hacknut – pevný stav
        if (_hacked)
        {
            prompt.Show("HACKED", ColorHacked);
            return;
        }

        PlayerMode mode = PlayerModeController.Instance.CurrentMode;

        // ── Normal mode: zobraz "Authorization Required" (nebo SO text) ────
        if (mode != PlayerMode.Hack)
        {
            string lockedText = config != null ? config.lockedText : "Encrypted: Hack Required";
            prompt.Show(lockedText, ColorLocked);
            return;
        }

        // ── Hack mode: zkontroluj vzdálenost ───────────────────────────────
        float hackRange = config != null ? config.serverHackRange : 15f;
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist > hackRange)
        {
            string outText = config != null ? config.outOfRangeText : "OUT OF RANGE";
            prompt.Show(outText, ColorOutOfRange);
        }
        else
        {
            string key = config != null ? config.interactKey : "E";
            string hackText = config != null ? config.hackText : "HACK";
            prompt.Show($"[{key}] {hackText}", ColorHack);
        }
    }
}