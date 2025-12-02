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
        bool isOpen,
        float distance,
        DoorInteractionConfig config)
    {
        if (mode == PlayerMode.Normal)
            return ResolveNormalMode(isLocked, isOpen, distance, config);
        else
            return ResolveHackMode(isLocked, isOpen, distance, config);
    }

    private static InteractionResult ResolveNormalMode(
        bool isLocked,
        bool isOpen,
        float distance,
        DoorInteractionConfig config)
    {
        // Out of range - no prompt
        if (distance > config.physicalInteractionRange)
            return InteractionResult.NoPrompt();

        // In range but locked - show info
        if (isLocked)
            return InteractionResult.Locked(config.lockedText);

        // In range, unlocked, open - can close
        if (isOpen)
            return InteractionResult.Physical(config.physicalCloseText);

        // In range, unlocked, closed - can open
        return InteractionResult.Physical(config.physicalUseText);
    }

    private static InteractionResult ResolveHackMode(
        bool isLocked,
        bool isOpen,
        float distance,
        DoorInteractionConfig config)
    {
        // Out of range - show info
        if (distance > config.hackRange)
            return InteractionResult.Locked(config.outOfRangeText);

        // In range and locked - can hack
        if (isLocked)
            return InteractionResult.Hack(config.hackText);

        // In range, unlocked, open - can close
        if (isOpen)
            return InteractionResult.Physical(config.physicalCloseText);

        // In range, unlocked, closed - can open
        return InteractionResult.Physical(config.physicalUseText);
    }
}