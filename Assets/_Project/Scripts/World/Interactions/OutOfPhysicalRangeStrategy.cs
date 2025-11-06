using UnityEngine;

/// <summary>
/// Normal mode fallback: Player is in trigger but outside physical range.
/// Shows informational message without interaction.
/// </summary>
public class OutOfPhysicalRangeStrategy : IInteractionStrategy
{
    public bool CanExecute(DoorContext ctx)
    {
        // Fallback: always true (lowest priority)
        return true;
    }

    public bool CanInteract(DoorContext ctx) => false;

    public string GetPromptText(DoorContext ctx)
    {
        return "Too Far";
    }

    public void Execute(DoorContext ctx)
    {
        // Info only - player needs to get closer
    }
}