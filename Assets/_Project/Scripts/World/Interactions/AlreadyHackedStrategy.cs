using UnityEngine;

/// <summary>
/// Hack mode: Door is already unlocked - show info only.
/// </summary>
public class AlreadyUnlockedStrategy : MonoBehaviour, IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return !ctx.IsLocked && ctx.Distance <= ctx.Config.hackRange;
    }

    public bool CanInteract(DoorContext ctx) => false; // Info only

    public string GetPromptText(DoorContext ctx)
    {
        return "Already Unlocked";
    }

    public void Execute(DoorContext ctx)
    {
        // Informational only
        Debug.Log("[AlreadyUnlockedStrategy] Door already unlocked");
    }
}