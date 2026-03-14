using UnityEngine;

public class DoorClosedState : DoorState
{
    public DoorClosedState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // zvuk a animace už proběhly v DoorClosingState
        machine.Lock.StartAutoLock();
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