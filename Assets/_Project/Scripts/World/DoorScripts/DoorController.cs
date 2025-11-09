using UnityEngine;

/// <summary>
/// Handles door animations, sounds, and prompt display.
/// Pure presentation logic - no game logic.
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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Settings")]
    [SerializeField] private float animationDuration = 1.25f;

    public float AnimationDuration => animationDuration;

    private Transform player;
    private bool playerInRange;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void Open()
    {
        animator.SetBool(openBoolName, true);
        PlaySound(openSound);
    }

    public void Close()
    {
        animator.SetBool(openBoolName, false);
        PlaySound(closeSound);
    }

    public void SetPlayerInRange(Transform playerTransform, bool inRange)
    {
        player = playerTransform;
        playerInRange = inRange;

        if (!inRange)
            HidePrompts();
    }

    public void SetPromptEnabled(bool enabled, string promptText = "")
    {
        if (!playerInRange || !enabled || string.IsNullOrEmpty(promptText))
        {
            HidePrompts();
            return;
        }

        ShowPromptForSide(promptText);
    }

    private void ShowPromptForSide(string text)
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
            promptBack?.Show(text);
            promptFront?.Hide();
        }
        else
        {
            promptFront?.Show(text);
            promptBack?.Hide();
        }
    }

    public void HidePrompts()
    {
        promptFront?.Hide();
        promptBack?.Hide();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}