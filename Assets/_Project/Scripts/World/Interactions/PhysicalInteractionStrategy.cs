using UnityEngine;

public class PhysicalInteractionStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return !ctx.IsLocked && ctx.Distance <= ctx.Config.physicalInteractionRange;
    }

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.physicalUseText;
    }

    public void Execute(DoorContext ctx)
    {
        ctx.StateMachine.OnInteract();
    }
}