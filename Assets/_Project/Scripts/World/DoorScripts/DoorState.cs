using UnityEngine;

public abstract class DoorState
{
    protected DoorController door;

    public DoorState(DoorController door)
    {
        this.door = door;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }

    public abstract void Interact();
}
