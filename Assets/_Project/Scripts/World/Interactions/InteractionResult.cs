using UnityEngine;

/// <summary>
/// Value object representing result of interaction resolution.
/// Immutable data holder - no logic.
/// </summary>
public readonly struct InteractionResult
{
    public readonly bool CanInteract;
    public readonly bool ShowPrompt;
    public readonly string PromptText;
    public readonly InteractionType Type;

    public InteractionResult(bool canInteract, bool showPrompt, string promptText, InteractionType type)
    {
        CanInteract = canInteract;
        ShowPrompt = showPrompt;
        PromptText = promptText;
        Type = type;
    }

    // Factory methods for common results
    public static InteractionResult Physical(string text) =>
        new InteractionResult(true, true, text, InteractionType.Physical);

    public static InteractionResult Hack(string text) =>
        new InteractionResult(true, true, text, InteractionType.Hack);

    public static InteractionResult Locked(string text) =>
        new InteractionResult(false, true, text, InteractionType.Locked);

    public static InteractionResult NoPrompt() =>
        new InteractionResult(false, false, "", InteractionType.None);
}

public enum InteractionType
{
    None,
    Physical,
    Hack,
    Locked
}