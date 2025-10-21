using UnityEngine;

public class DoorClosedState : DoorState
{
    public DoorClosedState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        Debug.Log("Door is now in ClosedState.");
        door.SetAnimatorBool(false);
        door.ShowPromptForSide(door.GetInteractText());

        // Start the auto-lock timer: after configured inactivity the door will return to LockedState.
        door.StartAutoLock();
    }

    public override void Exit()
    {
        // Stop the auto-lock when we leave closed state
        door.StopAutoLock();
    }

    public override void Interact()
    {
        door.HidePrompts();
        door.SetState(new DoorOpeningState(door));
    }
}
