using UnityEngine;

public class DoorClosingState : DoorState
{
    private float timer;

    public DoorClosingState(DoorController door) : base(door) { }

    public override void Enter()
    {
        Debug.Log("Door is now in ClosingState.");
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
