using UnityEngine;

public class DoorOpeningState : DoorState
{
    private float timer;

    public DoorOpeningState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        // If the player opens the door, cancel any scheduled auto-lock.
        door.CancelPendingLock();

        door.SetAnimatorBool(true);
        timer = door.Cooldown;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            door.SetState(new DoorOpenState(door));
        }
    }

    public override void Interact() { } 
}
