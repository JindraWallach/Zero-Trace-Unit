/// <summary>
/// Hack mode: Door is unlocked - can open physically via hack mode.
/// This allows opening doors in hack mode even when unlocked.
/// </summary>
public class HackModeUnlockedStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return !ctx.IsLocked && ctx.Distance <= ctx.Config.hackRange;
    }

    public bool CanInteract(DoorContext ctx) => true;

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.physicalUseText; // "Open" - same as physical
    }

    public void Execute(DoorContext ctx)
    {
        //Debug.Log("[HackModeUnlockedStrategy] Opening unlocked door in hack mode");
        ctx.StateMachine.OnInteract();
    }
}