using UnityEngine;

public class DoorLockedState : DoorState
{
    public DoorLockedState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        // Ensure door appears closed and show locked prompt
        door.SetAnimatorBool(false);
        Debug.Log("Door is now in LockedState.");
        door.ShowPromptForSide(door.GetLockedText());
    }

    public override void Exit()
    {
        door.HidePrompts();
    }

    // Simulate starting the hack mini‑game and immediately completing it (per your request).
    // This will unlock the door and transition into ClosedState.
    public override void Interact()
    {
        door.HidePrompts();
        // Simulate hack success
        Debug.Log("Hack successful! Door is now unlocked.");
        door.OnHackSuccess();
    }
}