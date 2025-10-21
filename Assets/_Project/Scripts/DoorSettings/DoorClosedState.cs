using UnityEngine;

public class DoorClosedState : DoorState
{
    public DoorClosedState(DoorInteractable door) : base(door) { }

    public override void Enter()
    {
        door.SetAnimatorBool(false);
        door.ShowPromptForSide();
    }

    public override void Interact()
    {
        door.HidePrompts();
        door.SetState(new DoorOpeningState(door));
    }
}
