public abstract class DoorState
{
    protected DoorStateMachine machine;

    public DoorState(DoorStateMachine machine)
    {
        this.machine = machine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public abstract void Interact();
}