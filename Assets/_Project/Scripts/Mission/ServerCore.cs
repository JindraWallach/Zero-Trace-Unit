// Scripts/Mission/ServerCore.cs
using System;
using UnityEngine;

/// <summary>
/// ServerCore – hackovatelný terminál mise.
///
//// Stejný pattern jako HackableDoor:
////   InteractableBase  → IInteractable (PlayerInteractor ho najde v range)
////   IHackTarget       → HackManager ho registruje, spouští puzzle
////   IInitializable    → DI zavolá Initialize(di)
///
//// Prompt logika:
////   Pouze 1 UIPromptController (terminál má jen 1 stranu).
////   Reaguje na OnModeChanged event – coroutine polling distance 5x/s.
////   Stavy: OUT OF RANGE (červená) | [E] HACK (žlutá) | ALREADY HACKED (šedá)
///
//// Po úspěšném hacknutí volá MissionManager.OnServerHacked().
/// </summary>
[DisallowMultipleComponent]
public class ServerCore : InteractableBase, IHackTarget, IInitializable
{
    [Header("Config")]
    [SerializeField] private MissionSystemConfig config;

    [Header("Hack")]
    [Tooltip("PuzzleDefinition SO – stejný jako u HackableDoor")]
    [SerializeField] private PuzzleDefinition puzzleDefinition;

    [Header("Prompt")]
    [Tooltip("UIPromptController na terminálu – 1 prompt, ne 2 jako u dveří")]
    [SerializeField] private UIPromptController prompt;

    [Tooltip("Jak často se updatuje prompt (s) – stejný pattern jako DoorInteractionMode")]
    [SerializeField] private float updateInterval = 0.2f;

    // ── IHackTarget ────────────────────────────────────────────────────────
    public string TargetID => config != null ? config.serverTargetID : "ServerCore";
    public bool IsHackable => !_hacked;

    // ── Interní state ──────────────────────────────────────────────────────
    private bool _hacked;
    private Transform _player;
    private bool _playerInRange;
    private Coroutine _updateCoroutine;

    // Barvy – stejné jako DoorInteractionConfig
    private static readonly Color ColorHack = new Color(1f, 0.8f, 0f);   // žlutá
    private static readonly Color ColorOutOfRange = new Color(1f, 0.2f, 0.2f); // červená
    private static readonly Color ColorHacked = new Color(0.5f, 0.5f, 0.5f); // šedá

    // ── IInitializable ─────────────────────────────────────────────────────

    public void Initialize(DependencyInjector di)
    {
        HackManager.Instance?.RegisterTarget(this);

        // Poslouchej změnu modu – ihned refresh prompt (stejně jako DoorInteractionMode)
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;
    }

    private void OnDestroy()
    {
        HackManager.Instance?.UnregisterTarget(this);

        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;

        StopUpdating();
    }

    // ── IInteractable (přes InteractableBase) ──────────────────────────────

    /// <summary>
    /// Fyzická interakce – server nejde otevřít ručně, nic nedělej.
    /// </summary>
    public override void Interact()
    {
        // PlayerInteractor volá Interact() při stisku E
        // Server nemá fyzickou interakci – hack se spouští vždy přes RequestHack
        if (PlayerModeController.Instance.CurrentMode != PlayerMode.Hack) return;
        if (!_playerInRange) return;

        RequestHack(
            onSuccess: null,
            onFail: () => Debug.Log("[ServerCore] Hack failed."),
            onCancel: () => Debug.Log("[ServerCore] Hack cancelled.")
        );
    }

    public override void OnEnterRange()
    {
        base.OnEnterRange();
        // Do not attempt to show prompt here using internal _player (not yet set).
        // PlayerInteractor will call ShowPromptForPlayer(playerTransform) with the correct transform.
    }

    public override void OnExitRange()
    {
        base.OnExitRange();
        HidePromptForPlayer();
    }

    public override void ShowPromptForPlayer(Transform player)
    {
        _player = player;
        _playerInRange = true;
        StartUpdating();

        if (config != null && config.debugLog)
            Debug.Log($"[ServerCore] ShowPromptForPlayer called. player={(player == null ? "null" : player.name)}");
    }

    public override void HidePromptForPlayer()
    {
        _player = null;
        _playerInRange = false;
        StopUpdating();
        prompt?.Hide();
    }

    // ── IHackTarget.RequestHack ────────────────────────────────────────────

    public void RequestHack(Action onSuccess, Action onFail, Action onCancel = null)
    {
        if (PlayerModeController.Instance.CurrentMode != PlayerMode.Hack)
        {
            onFail?.Invoke();
            return;
        }

        if (!IsHackable)
        {
            onFail?.Invoke();
            return;
        }

        bool started = HackManager.Instance.RequestHack(
            this,
            onSuccess: () => { onSuccess?.Invoke(); HandleHackSuccess(); },
            onFail: onFail,
            onCancel: onCancel
        );

        if (!started)
            onFail?.Invoke();
    }

    /// <summary>Accessor pro PuzzleFactory – stejný vzor jako HackableDoor.</summary>
    public PuzzleDefinition GetPuzzleDefinition() => puzzleDefinition;

    // ── Prompt update (coroutine, 5x/s – stejně jako DoorInteractionMode) ──

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
    }

    private System.Collections.IEnumerator UpdateCoroutine()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (_playerInRange && _player != null)
        {
            RefreshPrompt();
            yield return wait;
        }
    }

    /// <summary>Voláno při změně PlayerMode – okamžitý refresh bez čekání na interval.</summary>
    private void OnModeChanged(PlayerMode _)
    {
        if (_playerInRange)
            RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (prompt == null || _player == null) return;

        // Již hacknutý
        if (_hacked)
        {
            prompt.Show("HACKED", ColorHacked);
            return;
        }

        // Mimo Hack mode – prompt schovej
        if (PlayerModeController.Instance.CurrentMode != PlayerMode.Hack)
        {
            prompt.Hide();
            return;
        }

        // Hack mode – zkontroluj vzdálenost
        float distance = Vector3.Distance(transform.position, _player.position);
        float hackRange = config != null ? config.serverHackRange : 15f;

        if (distance > hackRange)
        {
            prompt.Show("OUT OF RANGE", ColorOutOfRange);
        }
        else
        {
            string key = config != null ? config.interactKey : "E";
            prompt.Show($"[{key}] HACK", ColorHack);
        }

        if (config != null && config.debugLog)
            Debug.Log($"[ServerCore] RefreshPrompt called. dist={Vector3.Distance(transform.position, _player.position):F2}");
    }

    // ── Interní ────────────────────────────────────────────────────────────

    private void HandleHackSuccess()
    {
        if (_hacked) return;
        _hacked = true;

        // Prompt na "HACKED" stav
        prompt?.Show("HACKED", ColorHacked);

        if (config != null && config.debugLog)
            Debug.Log("[ServerCore] Hack success – notifying MissionManager.");

        if (MissionManager.Instance == null)
        {
            Debug.LogError("[ServerCore] MissionManager not found!");
            return;
        }

        MissionManager.Instance.OnServerHacked(transform.position);
    }
}