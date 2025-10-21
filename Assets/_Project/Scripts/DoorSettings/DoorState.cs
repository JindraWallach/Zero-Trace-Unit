using UnityEngine;

public abstract class DoorState
{
    protected DoorInteractable door;

    public DoorState(DoorInteractable door)
    {
        this.door = door;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }

    public abstract void Interact();
}
