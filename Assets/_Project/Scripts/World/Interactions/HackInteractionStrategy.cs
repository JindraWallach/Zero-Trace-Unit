using UnityEngine;

public class HackInteractionStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return ctx.IsLocked && ctx.Distance <= ctx.Config.hackRange;
    }

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.hackText;
    }

    public void Execute(DoorContext ctx)
    {
        ctx.HackableDoor.RequestHack(
            onSuccess: () => ctx.StateMachine.Lock.Unlock(),
            onFail: () => Debug.Log("Hack failed")
        );
    }
}