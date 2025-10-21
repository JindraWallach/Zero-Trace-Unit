using UnityEngine;

public class DoorOpenState : DoorState
{
    private float timer;
    public DoorOpenState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        Debug.Log("Door is now in OpenState.");
        door.SetAnimatorBool(true);
        timer = door.AutoCloseDelay;
    }

    public override void Interact()
    {
        door.SetState(new DoorClosingState(door));
    }
}
