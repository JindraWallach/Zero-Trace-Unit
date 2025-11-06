/// <summary>
/// Normal mode: Physical interaction when unlocked and in range.
/// </summary>
public class PhysicalInteractionStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return !ctx.IsLocked && ctx.Distance <= ctx.Config.physicalInteractionRange;
    }

    public bool CanInteract(DoorContext ctx) => true;

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.physicalUseText;
    }

    public void Execute(DoorContext ctx)
    {
        //Debug.Log("[PhysicalInteractionStrategy] Opening door physically");
        ctx.StateMachine.OnInteract();
    }
}