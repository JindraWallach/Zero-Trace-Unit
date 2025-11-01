public class AlreadyHackedStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return !ctx.IsLocked && ctx.Distance <= ctx.Config.hackRange;
    }

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.alreadyHackedText;
    }

    public void Execute(DoorContext ctx)
    {
        // Informational only
    }
}