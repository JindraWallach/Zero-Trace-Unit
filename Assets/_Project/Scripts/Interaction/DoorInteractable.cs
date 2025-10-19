using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections;
using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [SerializeField] private Animator animator;
    [SerializeField] public float autoCloseDelay = 10f;

    public float offset = 1.25f;

    private bool isOpen;
    private bool isChanging;
    private string booleanAnimName = "IsOpen";

    public void Initialize(InputReader reader)
    {
        reader.onInteract += Interact;
    }

    public override void Interact()
    {
        if (isChanging) return;

        isOpen = !isOpen;
        animator.SetBool(booleanAnimName, isOpen);
        StopAllCoroutines();

        if (isOpen)
            StartCoroutine(AutoCloseAfterDelay());

        StartCoroutine(InteractionCooldown());
    }

    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isOpen) 
        {
            isOpen = false;
            animator.SetBool(booleanAnimName, false);
        }
    }

    private IEnumerator InteractionCooldown()
    {
        isChanging = true;
        yield return new WaitForSeconds(offset);
        isChanging = false;
    }
}
