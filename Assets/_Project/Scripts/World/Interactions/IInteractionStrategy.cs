using UnityEngine;

/// <summary>
/// Strategy interface for door interactions.
/// Implementations must be plain C# classes - NOT MonoBehaviours!
/// </summary>
public interface IInteractionStrategy
{
    bool CanExecute(DoorContext context);

    bool CanInteract(DoorContext context);

    string GetPromptText(DoorContext context);

    void Execute(DoorContext context);
}

/// <summary>
/// Context data holder for door interactions.
/// Passed to all strategies.
/// </summary>
public class DoorContext
{
    //TODO : add show prompttextUI boolean
    public Transform Player;
    public float Distance;
    public bool IsLocked;
    public PlayerMode CurrentMode;
    public DoorStateMachine StateMachine;
    public HackableDoor HackableDoor;
    public DoorInteractionConfig Config;
}