using UnityEngine;

public interface IInteractionStrategy
{
    bool CanExecute(DoorContext context);
    string GetPromptText(DoorContext context);
    void Execute(DoorContext context);
}

// Context data holder
public class DoorContext
{
    public Transform Player;
    public float Distance;
    public bool IsLocked;
    public DoorStateMachine StateMachine;
    public HackableDoor HackableDoor;
    public DoorInteractionConfig Config;
}