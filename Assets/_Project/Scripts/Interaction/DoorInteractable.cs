using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections;
using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private InteractionPromptUI promptFront;
    [SerializeField] private InteractionPromptUI promptBack;
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float interactionCooldown = 1.25f;
    [SerializeField] private float autoCloseDelay = 3f;
    [SerializeField] private bool invertSideLogic = false; // Pro obrácené dveře

    private InputReader inputReader;
    private bool isOpen = false;
    private bool isChanging = false;
    private bool playerInRange = false;
    private Coroutine autoCloseCoroutine;
    private Coroutine interactionCooldownCoroutine;

    private const string booleanAnimName = "IsOpen";

    protected override void Awake()
    {
        base.Awake();
        HideBothPrompts();
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.onInteract -= Interact;
    }

    public void Initialize(InputReader reader)
    {
        inputReader = reader;
        inputReader.onInteract += Interact;
    }

    public override void OnEnterRange()
    {
        playerInRange = true;
        if (!isOpen && !isChanging)
            ShowPromptForSide();
    }

    public override void OnExitRange()
    {
        playerInRange = false;
        HideBothPrompts();
    }

    public override void Interact()
    {
        if (isChanging) return;

        ToggleDoor();
        HideBothPrompts();
        StopActiveCoroutines();

        interactionCooldownCoroutine = StartCoroutine(InteractionCooldown());

        if (isOpen)
            autoCloseCoroutine = StartCoroutine(AutoClose());
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;
        animator.SetBool(booleanAnimName, isOpen);
    }

    private void StopActiveCoroutines()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        if (interactionCooldownCoroutine != null)
        {
            StopCoroutine(interactionCooldownCoroutine);
            interactionCooldownCoroutine = null;
        }
    }

    private void ShowPromptForSide()
    {
        Vector3 localPlayerPos = transform.InverseTransformPoint(player.position);
        bool shouldShowBack = localPlayerPos.z >= 0;

        if (invertSideLogic)
            shouldShowBack = !shouldShowBack;

        if (shouldShowBack)
        {
            promptBack.Show(interactText);
            promptFront.Hide();
            //Debug.Log($"Door: {name} | Local Z: {localPlayerPos.z} | Showing Back Prompt | Inverted: {invertSideLogic}");
        }
        else
        {
            promptFront.Show(interactText);
            promptBack.Hide();
            //Debug.Log($"Door: {name} | Local Z: {localPlayerPos.z} | Showing Front Prompt | Inverted: {invertSideLogic}");
        }
    }

    private void HideBothPrompts()
    {
        promptFront.Hide();
        promptBack.Hide();
    }

    private IEnumerator InteractionCooldown()
    {
        isChanging = true;
        yield return new WaitForSeconds(interactionCooldown);
        isChanging = false;

        if (!isOpen && playerInRange)
            ShowPromptForSide();
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        if (!isOpen) yield break;

        isOpen = false;
        animator.SetBool(booleanAnimName, false);

        if (playerInRange)
            ShowPromptForSide();
    }
}