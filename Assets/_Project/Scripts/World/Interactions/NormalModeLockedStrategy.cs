/// <summary>
/// Normal mode: Door is locked - show info only, no interaction.
/// </summary>
public class NormalModeLockedStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return ctx.IsLocked && ctx.Distance <= ctx.Config.physicalInteractionRange;
    }

    public bool CanInteract(DoorContext ctx) => false;

    public string GetPromptText(DoorContext ctx)
    {
        return "Locked";
    }

    public void Execute(DoorContext ctx)
    {
        //Debug.Log("[NormalModeLockedStrategy] Door is locked - switch to Hack Mode");
    }
}