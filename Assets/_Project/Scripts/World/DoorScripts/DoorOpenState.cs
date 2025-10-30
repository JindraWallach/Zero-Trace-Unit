using UnityEngine;

public class DoorOpenState : DoorState
{
    public DoorOpenState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Open();
        Debug.Log("[DoorOpenState] Door open");
    }

    public override void Interact()
    {
        machine.SetState(new DoorClosingState(machine));
    }
}
