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

    [Header("Lock")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private float lockTimeout = 30f; // seconds until door re-locks when idle

    private DoorState currentState;
    private Coroutine autoCloseCoroutine;
    private Coroutine autoLockCoroutine;
    private bool isActive = true;
    private bool isLocked;

    public float Cooldown => interactionCooldown;
    public float AutoCloseDelay => autoCloseDelay;
    public bool IsLocked => isLocked;

    public void Initialize(InputReader reader)
    {
        reader.onInteract += Interact;

        isLocked = startLocked;
        if (isLocked)
            SetState(new DoorLockedState(this));
        else
            SetState(new DoorClosedState(this));
    }

    private void Update()
    {
        if (!isActive) return;
        currentState?.Update();
    }

    public override void Interact()
    {
        currentState?.Interact();
    }

    public override void OnEnterRange()
    {
        isActive = true;
        // only show prompt if closed (locked will show its own)
        if (currentState is DoorClosedState)
            ShowPromptForSide();
    }

    public override void OnExitRange()
    {
        isActive = false;
        HidePrompts();
    }

    public void SetState(DoorState newState)
    {
        // stop any pending auto-lock when changing states; a state may restart it if desired
        StopAutoLock();

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void SetAnimatorBool(bool isOpen)
    {
        animator.SetBool("IsOpen", isOpen);
    }

    // existing API kept for backward compatibility
    public void ShowPromptForSide()
    {
        ShowPromptForSide(GetInteractText());
    }

    // overload to show arbitrary text (locked/interact)
    public void ShowPromptForSide(string text)
    {
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

    // Lock / Unlock API used by states or external systems (e.g. hack success)
    public void Lock()
    {
        if (isLocked) return;
        isLocked = true;
        SetState(new DoorLockedState(this));
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;
        SetState(new DoorClosedState(this));
    }

    // Called by the hack system (or DoorLockedState.Interact simulation) to indicate hack success
    public void OnHackSuccess()
    {
        Unlock();
    }

    // Auto-close logic (existing)
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

    // Auto-lock logic: when the door is in ClosedState, you can start this timer to re-lock the door after inactivity
    public void StartAutoLock()
    {
        StopAutoLock();
        autoLockCoroutine = StartCoroutine(AutoLockCoroutine());
    }

    public void StopAutoLock()
    {
        if (autoLockCoroutine != null)
        {
            StopCoroutine(autoLockCoroutine);
            autoLockCoroutine = null;
        }
    }

    private IEnumerator AutoLockCoroutine()
    {
        yield return new WaitForSeconds(lockTimeout);

        // Only lock if we're currently in closed state and not already locked
        if (!isLocked && currentState is DoorClosedState)
        {
            SetState(new DoorLockedState(this));
            isLocked = true;
        }

        autoLockCoroutine = null;
    }

    public void HidePrompts()
    {
        promptFront.Hide();
        promptBack.Hide();
    }
}
