using UnityEngine;

public class DoorClosedState : DoorState
{
    public DoorClosedState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Close();
        var hackable = machine.GetComponent<HackableDoor>();
        string text = hackable?.GetInteractText() ?? "Use";
        machine.Controller.SetPromptEnabled(true, text);
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