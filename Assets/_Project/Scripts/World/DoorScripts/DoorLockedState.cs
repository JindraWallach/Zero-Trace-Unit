using UnityEngine;

public class DoorLockedState : DoorState
{
    public DoorLockedState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Close();
        var hackable = machine.GetComponent<HackableDoor>();
        string text = hackable?.GetLockedText() ?? "HACK";
        machine.Controller.SetPromptEnabled(true, text);
    }

    public override void Interact()
    {
        // Attempt hack via HackableDoor component
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