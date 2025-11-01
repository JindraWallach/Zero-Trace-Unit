using System;
using UnityEngine;

/// <summary>
/// Bridge between door and hack system.
/// Registers as IHackTarget with HackManager.
/// Does NOT handle UI or puzzles directly.
/// </summary>
public class HackableDoor : InteractableBase, IHackTarget, IInitializable
{
    [Header("Hack Config")]
    [SerializeField] private PuzzleDefinition puzzleDefinition;
    [SerializeField] private string targetID;

    private DoorStateMachine stateMachine;
    private DoorInteractionMode interactionMode;
    private DependencyInjector dependencyInjector;

    public string TargetID => targetID;
    public bool IsHackable => GetComponent<DoorStateMachine>().Lock.IsLocked;

    protected override void Awake()
    {
        base.Awake();
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

    public override void OnEnterRange()
    {
        base.OnEnterRange();
        isInRange = true;
    }

    public override void OnExitRange()
    {
        base.OnExitRange();
        isInRange = false;
        GetComponent<DoorController>()?.SetPlayerInRange(null, false);
    }

    public void RequestHack(Action onSuccess, Action onFail)
    {
        if (!IsHackable)
        {
            onFail?.Invoke();
            return;
        }

        HackManager.Instance.BlockPlayerInput(dependencyInjector.InputReader);
        bool started = HackManager.Instance.RequestHack(this, onSuccess, onFail);

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

    public override string GetInteractText()
    {
        return stateMachine.Lock.IsLocked ? lockedText : interactText;
    }
}