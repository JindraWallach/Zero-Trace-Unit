/// <summary>
/// Hack mode: Target out of range.
/// </summary>
public class OutOfRangeStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return ctx.Distance > ctx.Config.hackRange;
    }

    public bool CanInteract(DoorContext ctx) => false;

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.outOfRangeText;
    }

    public void Execute(DoorContext ctx)
    {
        //Debug.Log("[OutOfRangeStrategy] Target out of range");
    }
}