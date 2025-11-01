using UnityEngine;

public class DoorClosingState : DoorState
{
    private float timer;

    public DoorClosingState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        machine.Controller.Close();
        machine.Controller.SetPromptEnabled(false);
        timer = machine.AnimDuration;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
            machine.SetState(new DoorClosedState(machine));
    }

    public override void Interact() { }
}