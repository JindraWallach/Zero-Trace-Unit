using UnityEngine;

/// <summary>
/// Pure static resolver for door interactions.
/// No state, no allocations - just pure functions.
/// Replaces Strategy pattern with simple if/else logic.
/// </summary>
public static class DoorInteractionResolver
{
    /// <summary>
    /// Resolves interaction based on current context.
    /// Returns what should happen and what UI to show.
    /// </summary>
    public static InteractionResult Resolve(
        PlayerMode mode,
        bool isLocked,
        float distance,
        DoorInteractionConfig config)
    {
        if (mode == PlayerMode.Normal)
            return ResolveNormalMode(isLocked, distance, config);
        else
            return ResolveHackMode(isLocked, distance, config);
    }

    private static InteractionResult ResolveNormalMode(
        bool isLocked,
        float distance,
        DoorInteractionConfig config)
    {
        // Out of range - no prompt
        if (distance > config.physicalInteractionRange)
            return InteractionResult.NoPrompt();

        // In range but locked - show info
        if (isLocked)
            return InteractionResult.InfoOnly("Locked");

        // In range and unlocked - can open
        return InteractionResult.Physical(config.physicalUseText);
    }

    private static InteractionResult ResolveHackMode(
        bool isLocked,
        float distance,
        DoorInteractionConfig config)
    {
        // Out of range - show info
        if (distance > config.hackRange)
            return InteractionResult.InfoOnly(config.outOfRangeText);

        // In range and locked - can hack
        if (isLocked)
            return InteractionResult.Hack(config.hackText);

        // In range and unlocked - can open
        return InteractionResult.Physical(config.physicalUseText);
    }
}