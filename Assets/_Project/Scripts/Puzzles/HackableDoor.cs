using System;
using UnityEngine;

/// <summary>
/// Bridge between door system and hack manager.
/// Validates player mode before allowing hack requests.
/// </summary>
public class HackableDoor : InteractableBase, IHackTarget, IInitializable
{
    [Header("Hack Config")]
    [SerializeField] private PuzzleDefinition puzzleDefinition;
    [SerializeField] private string targetID;

    private DoorInteractionMode interactionMode;
    private DependencyInjector dependencyInjector;

    public string TargetID => targetID;
    public bool IsHackable => GetComponent<DoorStateMachine>().Lock.IsLocked;

    protected void Awake()
    {
        interactionMode = GetComponent<DoorInteractionMode>();

        if (string.IsNullOrEmpty(targetID))
            targetID = $"Door_{GetInstanceID()}";
    }

    public void Initialize(DependencyInjector di)
    {
        dependencyInjector = di;
        HackManager.Instance?.RegisterTarget(this);
    }

    private void OnDestroy()
    {
        HackManager.Instance?.UnregisterTarget(this);
    }

    public override void Interact()
    {
        interactionMode.ExecuteInteraction();
    }

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

        bool started = HackManager.Instance.RequestHack(this, onSuccess, onFail, onCancel);

        if (!started)
            onFail?.Invoke();
    }

    public override void ShowPromptForPlayer(Transform player)
    {
        interactionMode?.SetPlayerInRange(player, true);
    }

    public override void HidePromptForPlayer()
    {
        interactionMode?.SetPlayerInRange(null, false);
    }
}