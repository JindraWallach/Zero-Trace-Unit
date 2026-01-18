using UnityEngine;

/// <summary>
/// Value object representing result of interaction resolution.
/// Now includes color for UI prompts.
/// Immutable data holder - no logic.
/// </summary>
public readonly struct InteractionResult
{
    public readonly bool CanInteract;
    public readonly bool ShowPrompt;
    public readonly string PromptText;
    public readonly Color PromptColor;
    public readonly InteractionType Type;

    public InteractionResult(
        bool canInteract,
        bool showPrompt,
        string promptText,
        Color promptColor,
        InteractionType type)
    {
        CanInteract = canInteract;
        ShowPrompt = showPrompt;
        PromptText = promptText;
        PromptColor = promptColor;
        Type = type;
    }

    // Factory methods for common results
    public static InteractionResult Physical(string text, Color color) =>
        new InteractionResult(true, true, text, color, InteractionType.Physical);

    public static InteractionResult Hack(string text, Color color) =>
        new InteractionResult(true, true, text, color, InteractionType.Hack);

    public static InteractionResult Locked(string text, Color color) =>
        new InteractionResult(false, true, text, color, InteractionType.Locked);

    public static InteractionResult NoPrompt() =>
        new InteractionResult(false, false, "", Color.clear, InteractionType.None);
}

public enum InteractionType
{
    None,
    Physical,
    Hack,
    Locked
}