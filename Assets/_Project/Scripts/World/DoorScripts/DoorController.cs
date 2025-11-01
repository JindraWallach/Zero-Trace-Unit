using UnityEngine;

/// <summary>
/// Controls door animation, sounds, and visual state.
/// Pure open/close logic without lock/hack concerns.
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
    private string currentPromptText = "";
    private bool playerInRange = false;
    private bool shouldShowPrompt = false;


    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    public void Open()
    {
        animator.SetBool(openBoolName, true);
        PlaySound(openSound);
        Debug.Log("[DoorController] Door opening");
    }

    public void Close()
    {
        animator.SetBool(openBoolName, false);
        PlaySound(closeSound);
        Debug.Log("[DoorController] Door closing");
    }

    public void SetPlayerInRange(Transform playerTransform, bool inRange)
    {
        player = playerTransform;
        playerInRange = inRange;

        if (!inRange)
            HidePrompts();
        else
            UpdatePromptVisibility();
    }

    public void SetPromptEnabled(bool enabled, string promptText = "")
    {
        shouldShowPrompt = enabled;
        currentPromptText = promptText; // Store text
        UpdatePromptVisibility();
    }

    private void UpdatePromptVisibility()
    {
        if (!playerInRange || !shouldShowPrompt)
        {
            HidePrompts();
            return;
        }

        ShowPromptForSide(currentPromptText);
    }

    public void ShowPromptForSide(string text)
    {
        if (!playerInRange || !shouldShowPrompt) return;

        Vector3 localPlayerPos = transform.InverseTransformPoint(player.position);
        bool shouldShowBack = localPlayerPos.z >= 0;

        if (invertSideLogic)
            shouldShowBack = !shouldShowBack;

        if (shouldShowBack)
        {
            promptBack.Show(text);
            promptFront.Hide();
        }
        else
        {
            promptFront.Show(text);
            promptBack.Hide();
        }
    }

    public void HidePrompts()
    {
        promptFront?.Hide();
        promptBack?.Hide();
    }

    public void SetPlayerReference(Transform playerTransform)
    {
        player = playerTransform;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}