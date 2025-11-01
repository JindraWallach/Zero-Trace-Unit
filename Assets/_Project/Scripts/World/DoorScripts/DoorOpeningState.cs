using UnityEngine;

public class DoorOpeningState : DoorState
{
    private float timer;

    public DoorOpeningState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Open();
        machine.Controller.SetPromptEnabled(false); // Hide prompt
        timer = machine.AnimDuration;
        Debug.Log("[DoorOpeningState] Door opening");
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            machine.SetState(new DoorOpenState(machine));
    }

    public override void Interact() { } // ignore during animation
}