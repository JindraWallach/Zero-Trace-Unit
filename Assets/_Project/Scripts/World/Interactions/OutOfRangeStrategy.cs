public class OutOfRangeStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return ctx.Distance > ctx.Config.hackRange;
    }

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.outOfRangeText;
    }

    public void Execute(DoorContext ctx)
    {
        // Can't interact
    }
}