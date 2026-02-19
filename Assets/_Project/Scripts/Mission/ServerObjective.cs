// Scripts/Mission/ServerCore.cs
using System;
using UnityEngine;

/// <summary>
/// ServerCore – hackovatelný objekt mise.
///
/// Stejný pattern jako HackableDoor:
///   InteractableBase  → IInteractable (PlayerInteractor ho najde v range)
///   IHackTarget       → HackManager ho registruje, spouští puzzle
///   IInitializable    → DI zavolá Initialize(di), zaregistruje do HackManageru
///
/// Po úspěšném hacknutí volá MissionManager.OnServerHacked().
/// </summary>
[DisallowMultipleComponent]
public class ServerCore : InteractableBase, IHackTarget
{
    [Header("Config")]
    [SerializeField] private MissionSystemConfig config;

    [Header("Hack")]
    [Tooltip("PuzzleDefinition SO – stejný jako u HackableDoor")]
    [SerializeField] private PuzzleDefinition puzzleDefinition;

    // ── IHackTarget ────────────────────────────────────────────────────────
    public string TargetID => config != null ? config.serverTargetID : "ServerCore";
    public bool IsHackable => !_hacked;

    private bool _hacked;

    // ── IInitializable ─────────────────────────────────────────────────────

    public void Start()
    {
        HackManager.Instance?.RegisterTarget(this);

        if (config != null && config.debugLog)
            Debug.Log($"[ServerCore] Registered with HackManager. ID: {TargetID}");
    }

    private void OnDestroy()
    {
        HackManager.Instance?.UnregisterTarget(this);
    }

    // ── IInteractable (přes InteractableBase) ──────────────────────────────

    public override void Interact()
    {
        // Fyzická interakce – server nelze otevřít fyzicky, nic nedělej
    }

    public override void ShowPromptForPlayer(Transform player)
    {
        // Zde můžeš zobrazit outline nebo prompt přes tvůj existující systém
        // (stejný vzor jako DoorInteractionMode)
        Debug.LogWarning("[ServerCore] Show prompt for player.");
    }

    public override void HidePromptForPlayer()
    {
        // Skryj outline / prompt
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
            onSuccess: () => { onSuccess?.Invoke(); OnHackSuccess(); },
            onFail: onFail,
            onCancel: onCancel
        );

        if (!started)
            onFail?.Invoke();
    }

    /// <summary>
    /// Public accessor pro PuzzleFactory – stejný vzor jako HackableDoor.GetPuzzleDefinition()
    /// </summary>
    public PuzzleDefinition GetPuzzleDefinition() => puzzleDefinition;

    // ── Interní ────────────────────────────────────────────────────────────

    private void OnHackSuccess()
    {
        if (_hacked) return;
        _hacked = true;

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