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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Settings")]
    [SerializeField] private float animationDuration = 1.25f;

    public float AnimationDuration => animationDuration;

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

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}