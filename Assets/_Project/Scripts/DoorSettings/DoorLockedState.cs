using UnityEngine;

public class DoorLockedState : DoorState
{
    public DoorLockedState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        door.SetAnimatorBool(false);
        door.ShowPromptForSide(door.GetLockedText());
    }

    public override void Exit()
    {
        door.HidePrompts();
    }

    public override void Interact()
    {
        // Optionally trigger feedback or a hack flow here.
    }
}