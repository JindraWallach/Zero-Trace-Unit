using UnityEngine;

/// <summary>
/// Pure static resolver for door interactions.
/// No state, no allocations - just pure functions.
/// Now returns formatted text with colors from config.
/// </summary>
public static class DoorInteractionResolver
{
    /// <summary>
    /// Resolves interaction based on current context.
    /// Returns what should happen, what UI to show, and what color to use.
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

        // In range but locked - show red locked text (no key shown)
        if (isLocked)
        {
            // Don't format with key for locked state (just info)
            return InteractionResult.Locked(
                config.lockedText,
                config.lockedColor
            );
        }

        // In range, unlocked, open - can close (green)
        if (isOpen)
        {
            string formattedText = config.FormatPrompt(config.physicalCloseText);
            return InteractionResult.Physical(
                formattedText,
                config.physicalColor
            );
        }

        // In range, unlocked, closed - can open (green)
        string openText = config.FormatPrompt(config.physicalUseText);
        return InteractionResult.Physical(
            openText,
            config.physicalColor
        );
    }

    private static InteractionResult ResolveHackMode(
        bool isLocked,
        bool isOpen,
        float distance,
        DoorInteractionConfig config)
    {
        // Out of range - show red info (no interaction)
        if (distance > config.hackRange)
        {
            return InteractionResult.Locked(
                config.outOfRangeText,
                config.lockedColor
            );
        }

        // In range and locked - can hack (yellow/orange)
        if (isLocked)
        {
            string formattedText = config.FormatPrompt(config.hackText);
            return InteractionResult.Hack(
                formattedText,
                config.hackColor
            );
        }

        // In range, unlocked, open - can close (green)
        if (isOpen)
        {
            string formattedText = config.FormatPrompt(config.physicalCloseText);
            return InteractionResult.Physical(
                formattedText,
                config.physicalColor
            );
        }

        // In range, unlocked, closed - can open (green)
        string openText = config.FormatPrompt(config.physicalUseText);
        return InteractionResult.Physical(
            openText,
            config.physicalColor
        );
    }
}