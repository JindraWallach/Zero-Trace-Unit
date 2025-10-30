using UnityEngine;

public class DoorOpenState : DoorState
{
    public DoorOpenState(DoorController door) : base(door) { }

    public override void Enter()
    {
        Debug.Log("Door is now in OpenState.");
        door.SetAnimatorBool(true);
        door.StartAutoLock();
    }

    public override void Exit()
    {
        door.StopAutoLock();
    }

    public override void Interact()
    {
        door.SetState(new DoorClosingState(door));
    }
}
