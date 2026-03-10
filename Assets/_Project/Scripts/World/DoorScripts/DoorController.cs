using UnityEngine;
using ZeroTrace.Audio;

/// <summary>
/// Handles door animations, sounds, and prompt display.
/// Pure presentation logic - no game logic.
/// Now supports colored prompts via InteractionResult.
/// Audio přes AudioManager — žádný vlastní AudioSource/AudioClip.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DoorController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openBoolName = "IsOpen";

    [Header("UI Prompts")]
    [SerializeField] private UIPromptController promptFront;
    [SerializeField] private UIPromptController promptBack;
    [SerializeField] private bool invertSideLogic = false;

    [Header("Audio IDs")]
    [Tooltip("AudioData id pro zvuk otevření (musí existovat v AudioManager)")]
    [SerializeField] private string openSoundId = "door_open";
    [Tooltip("AudioData id pro zvuk zavření (musí existovat v AudioManager)")]
    [SerializeField] private string closeSoundId = "door_close";

    [Header("Settings")]
    [SerializeField] private float animationDuration = 1.25f;

    public float AnimationDuration => animationDuration;

    private Transform player;
    private bool playerInRange;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Open()
    {
        animator.SetBool(openBoolName, true);
        AudioManager.Instance?.Play(openSoundId, transform.position);
        NoiseSystem.Instance?.EmitNoise(transform.position, 8f, NoiseType.DoorOpen);
    }

    public void Close()
    {
        animator.SetBool(openBoolName, false);
        AudioManager.Instance?.Play(closeSoundId, transform.position);
        NoiseSystem.Instance?.EmitNoise(transform.position, 6f, NoiseType.DoorClose);
    }

    public void SetPlayerInRange(Transform playerTransform, bool inRange)
    {
        player = playerTransform;
        playerInRange = inRange;

        if (!inRange)
            HidePrompts();
    }

    /// <summary>
    /// Set prompt with InteractionResult (includes color + formatted text).
    /// </summary>
    public void SetPrompt(InteractionResult result)
    {
        if (!playerInRange || !result.ShowPrompt || string.IsNullOrEmpty(result.PromptText))
        {
            HidePrompts();
            return;
        }

        ShowPromptForSide(result.PromptText, result.PromptColor);
    }

    /// <summary>
    /// Legacy method - kept for backwards compatibility.
    /// Use SetPrompt(InteractionResult) instead.
    /// </summary>
    public void SetPromptEnabled(bool enabled, string promptText = "")
    {
        if (!playerInRange || !enabled || string.IsNullOrEmpty(promptText))
        {
            HidePrompts();
            return;
        }

        ShowPromptForSide(promptText, Color.white);
    }

    public void HidePrompts()
    {
        promptFront?.Hide();
        promptBack?.Hide();
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void ShowPromptForSide(string text, Color color)
    {
        if (player == null)
        {
            HidePrompts();
            return;
        }

        Vector3 localPlayerPos = transform.InverseTransformPoint(player.position);
        bool shouldShowBack = localPlayerPos.z >= 0;

        if (invertSideLogic)
            shouldShowBack = !shouldShowBack;

        if (shouldShowBack)
        {
            promptBack?.Show(text, color);
            promptFront?.Hide();
        }
        else
        {
            promptFront?.Show(text, color);
            promptBack?.Hide();
        }
    }
}