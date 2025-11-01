using UnityEngine;

public class DoorClosedState : DoorState
{
    public DoorClosedState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Close();
        machine.Lock.StartAutoLock();
        // Prompt handled by DoorInteractionMode
    }

    public override void Exit()
    {
        machine.Lock.StopAutoLock();
    }

    public override void Interact()
    {
        machine.SetState(new DoorOpeningState(machine));
    }
}