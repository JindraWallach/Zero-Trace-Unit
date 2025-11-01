// Scripts/World/HackableDoor.cs
using System;
using UnityEngine;

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
}