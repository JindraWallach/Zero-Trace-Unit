using UnityEngine;
using System;

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

    // When interacting, try to launch the per-door puzzle (via DoorPuzzleLauncher).
    // If none configured, fallback to simulated hack success.
    public override void Interact()
    {
        door.HidePrompts();

        var launcher = (door as Component)?.GetComponent<DoorPuzzleLauncher>();
        if (launcher != null)
        {
            bool started = launcher.TryStartPuzzle(() =>
            {
                Debug.Log("Hack successful via puzzle! Door is now unlocked.");
                door.OnHackSuccess();
            });

            if (started) return;
        }

        // fallback: no configured puzzle -> simulate hack success
        Debug.Log("No puzzle configured on this door; simulating hack success.");
        door.OnHackSuccess();
    }
}
