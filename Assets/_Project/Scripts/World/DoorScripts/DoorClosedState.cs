using UnityEngine;

public class DoorClosedState : DoorState
{
    public DoorClosedState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        door.SetAnimatorBool(false);
        Debug.Log("Door is now in ClosedState.");
        // If an auto-lock was scheduled to happen after close, consume it and immediately lock.
        if (door.ConsumePendingLock())
        {
            // consume and transition to locked state
            door.Lock();
            Debug.Log("DoorClosedState: consumed pending auto-lock");
            return;
        }

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
