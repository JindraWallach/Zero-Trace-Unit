using UnityEngine;

public class DoorLockedState : DoorState
{
    public DoorLockedState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Close();
        // Prompt handled by DoorInteractionMode
    }

    public override void Interact()
    {
        var hackable = machine.GetComponent<HackableDoor>();
        if (hackable != null)
        {
            hackable.RequestHack(
                onSuccess: () =>
                {
                    machine.Lock.Unlock();
                    machine.SetState(new DoorOpeningState(machine));
                },
                onFail: () => Debug.Log("Hack failed")
            );
        }
    }
}