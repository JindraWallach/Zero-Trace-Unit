using UnityEngine;

public class DoorClosingState : DoorState
{
    private float timer;

    public DoorClosingState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        door.SetAnimatorBool(false);
        timer = door.Cooldown;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            door.SetState(new DoorClosedState(door));
        }
    }

    public override void Interact() { } // ignoruj během animace
}
