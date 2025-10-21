using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections;

public class DoorInteractable : InteractableObject
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private InteractionPromptUI promptFront;
    [SerializeField] private InteractionPromptUI promptBack;
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float interactionCooldown = 1.25f;
    [SerializeField] private bool invertSideLogic = false;
    [SerializeField] private float autoCloseDelay = 3f;

    private DoorState currentState;
    private Coroutine autoCloseCoroutine;

    public float Cooldown => interactionCooldown;
    public float AutoCloseDelay => autoCloseDelay;

    public void Initialize(InputReader reader)
    {
        reader.onInteract += Interact;
        SetState(new DoorClosedState(this));
    }

    private void Update()
    {
        currentState?.Update();
    }

    public override void Interact()
    {
        currentState?.Interact();
    }

    public override void OnEnterRange()
    {
        if(currentState is DoorClosedState) ShowPromptForSide();
    }

    public override void OnExitRange()
    {
        promptFront.Hide();
        promptBack.Hide();
    }

    public void SetState(DoorState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void SetAnimatorBool(bool isOpen)
    {
        animator.SetBool("IsOpen", isOpen);
    }

    public void ShowPromptForSide()
    {
        Vector3 localPlayerPos = transform.InverseTransformPoint(player.position);
        bool shouldShowBack = localPlayerPos.z >= 0;

        if (invertSideLogic)
            shouldShowBack = !shouldShowBack;

        if (shouldShowBack)
        {
            promptBack.Show(interactText);
            promptFront.Hide();
        }
        else
        {
            promptFront.Show(interactText);
            promptBack.Hide();
        }
    }

    public void AutoClose()
    {
        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        autoCloseCoroutine = StartCoroutine(AutoCloseCoroutine());
    }

    private IEnumerator AutoCloseCoroutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (currentState is DoorOpenState)
        {
            SetState(new DoorClosingState(this));
        }
        autoCloseCoroutine = null;
    }

    public void HidePrompts()
    {
        promptFront.Hide();
        promptBack.Hide();
    }
}
