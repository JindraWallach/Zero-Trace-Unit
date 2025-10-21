using UnityEngine;

public class DoorOpenState : DoorState
{
    public DoorOpenState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
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
