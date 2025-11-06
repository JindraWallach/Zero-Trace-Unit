using UnityEngine;

/// <summary>
/// Normal mode: Door is locked - show info only, no interaction.
/// </summary>
public class NormalModeLockedStrategy : MonoBehaviour, IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        return ctx.IsLocked && ctx.Distance <= ctx.Config.physicalInteractionRange;
    }

    public bool CanInteract(DoorContext ctx) => false; // Cannot interact in normal mode

    public string GetPromptText(DoorContext ctx)
    {
        return "Locked";
    }

    public void Execute(DoorContext ctx)
    {
        // No interaction possible
        Debug.Log("[NormalModeLockedStrategy] Door is locked - switch to Hack Mode");
    }
}