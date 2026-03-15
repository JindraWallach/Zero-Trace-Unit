// Scripts/Mission/ServerCore.cs
using System;
using UnityEngine;

/// <summary>
/// ServerCore – hackovatelný terminál mise.
///
/// SRP: Pouze IHackTarget + IInteractable + IInitializable.
/// Veškerá prompt logika delegována na ServerCoreInteractionMode (stejný pattern jako HackableDoor → DoorInteractionMode).
///
/// Závislosti:
///   InteractableBase  → IInteractable  (PlayerInteractor detekuje range)
///   IHackTarget       → HackManager registrace + spuštění puzzle
///   IInitializable    → DependencyInjector zavolá Initialize(di)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ServerCoreInteractionMode))]
public class ServerCore : InteractableBase, IHackTarget, IInitializable
{
    [Header("Config")]
    [SerializeField] private MissionSystemConfig config;

    [Header("Hack")]
    [Tooltip("PuzzleDefinition SO – stejný jako u HackableDoor")]
    [SerializeField] private PuzzleDefinition puzzleDefinition;

    // ── State ─────────────────────────────────────────────────────
    // ─────────
    private bool _hacked;
    private ServerCoreInteractionMode _interactionMode;

    // ── IHackTarget ────────────────────────────────────────────────────────
    public string TargetID => config != null ? config.serverTargetID : "ServerCore";
    public bool IsHackable => !_hacked;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _interactionMode = GetComponent<ServerCoreInteractionMode>();
    }

    private void OnDestroy()
    {
        HackManager.Instance?.UnregisterTarget(this);
    }

    // ── IInitializable ─────────────────────────────────────────────────────

    public void Initialize(DependencyInjector di)
    {
        HackManager.Instance?.RegisterTarget(this);
    }

    // ── IInteractable (přes InteractableBase) ──────────────────────────────

    public override void Interact()
    {
        _interactionMode?.ExecuteInteraction();
    }

    public override void ShowPromptForPlayer(Transform player)
    {
        _interactionMode?.SetPlayerInRange(player, inRange: true);
    }

    public override void HidePromptForPlayer()
    {
        _interactionMode?.SetPlayerInRange(null, inRange: false);
    }

    // ── IHackTarget ────────────────────────────────────────────────────────

    public void RequestHack(Action onSuccess, Action onFail, Action onCancel = null)
    {
        if (PlayerModeController.Instance.CurrentMode != PlayerMode.Hack)
        {
            onFail?.Invoke();
            return;
        }

        if (!IsHackable) //vibecoding ahh
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

        if (!started) onFail?.Invoke();
    }

    // ── Public accessors ───────────────────────────────────────────────────

    /// <summary>Accessor pro PuzzleFactory.</summary>
    public PuzzleDefinition GetPuzzleDefinition() => puzzleDefinition;

    // ── Private ────────────────────────────────────────────────────────────

    private void HandleHackSuccess()
    {
        if (_hacked) return;
        _hacked = true;

        _interactionMode?.OnHackSuccess();

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