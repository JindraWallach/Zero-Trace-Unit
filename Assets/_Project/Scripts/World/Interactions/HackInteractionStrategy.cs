using UnityEngine;

/// <summary>
/// Hack mode: Door is locked and in range - can hack.
/// </summary>
public class HackInteractionStrategy : MonoBehaviour, IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return ctx.IsLocked && ctx.Distance <= ctx.Config.hackRange;
    }

    public bool CanInteract(DoorContext ctx) => true;

    public string GetPromptText(DoorContext ctx)
    {
        return ctx.Config.hackText;
    }

    public void Execute(DoorContext ctx)
    {
        ctx.HackableDoor.RequestHack(
            onSuccess: () =>
            {
                ctx.StateMachine.Lock.Unlock();

                // BONUS: Auto-open after successful hack if enabled
                if (ctx.StateMachine.Lock.OpenAfterUnlock)
                {
                    Debug.Log("[HackInteractionStrategy] Auto-opening door after unlock");
                    ctx.StateMachine.SetState(new DoorOpeningState(ctx.StateMachine));
                }
            },
            onFail: () => Debug.Log("[HackInteractionStrategy] Hack failed")
        );
    }
}