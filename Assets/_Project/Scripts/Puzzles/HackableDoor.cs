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
    private bool isHackable = true;

    public string TargetID => targetID;
    public bool IsHackable => isHackable && stateMachine.Lock.IsLocked;

    protected override void Awake()
    {
        base.Awake();
        stateMachine = GetComponent<DoorStateMachine>();

        if (string.IsNullOrEmpty(targetID))
            targetID = $"Door_{GetInstanceID()}";
    }

    public void Initialize(DependencyInjector dependencyInjector)
    {
        // Register with HackManager
        HackManager.Instance?.RegisterTarget(this);
    }

    private void OnDestroy()
    {
        HackManager.Instance?.UnregisterTarget(this);
    }

    public override void Interact()
    {
        stateMachine.OnInteract();
    }

    public void RequestHack(Action onSuccess, Action onFail)
    {
        if (!IsHackable)
        {
            Debug.LogWarning($"[HackableDoor] {targetID} is not hackable");
            onFail?.Invoke();
            return;
        }

        bool started = HackManager.Instance.RequestHack(this, onSuccess, onFail);
        if (!started)
            onFail?.Invoke();
    }

    public override string GetInteractText()
    {
        return stateMachine.Lock.IsLocked ? lockedText : interactText;
    }
}