using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections;

public class DoorController : InteractableObject, IInitializable
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private InteractionPromptUI promptFront;
    [SerializeField] private InteractionPromptUI promptBack;


    [Header("Settings")]
    [SerializeField] private float interactionCooldown = 1.25f;
    [SerializeField] private bool invertSideLogic = false;

    [Header("Lock")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private float lockTimeout = 30f; // seconds until door re-locks when idle

    private DoorState currentState;
    private Coroutine autoLockCoroutine;
    private Transform player;
    private bool isActive = false;
    private bool isLocked;

    // When autolock triggers while the door is open (or in other transient states),
    // schedule an actual locking to happen after the door reaches ClosedState.
    private bool pendingLockAfterClose;

    public float Cooldown => interactionCooldown;
    public bool IsLocked => isLocked;

    public void Initialize(DependencyInjector dependencyInjector)
    {
        player = dependencyInjector.PlayerPosition;

        isLocked = startLocked;
        if (isLocked)
            SetState(new DoorLockedState(this));
        else
            SetState(new DoorClosedState(this));

        HidePrompts();
    }

    private void Update()
    {
        if (!isActive) return;
        currentState?.Update();
    }

    public override void Interact(GameObject player)
    {
        currentState?.Interact();
    }

    // OnEnterRange/OnExitRange now only control isActive; prompt visibility
    // is handled by ShowPromptForPlayer/HidePromptForPlayer called by the detector.
    public override void OnEnterRange(GameObject player)
    {
        isActive = true;
    }

    public override void OnExitRange(GameObject player)
    {
        isActive = false;
        HidePrompts();
    }

    public override void ShowPromptForPlayer(Transform player)
    {
        // store player for existing ShowPromptForSide logic, then show proper prompt
        this.player = player;
        ShowPromptForCurrentState();
    }

    public override void HidePromptForPlayer()
    {
        HidePrompts();
    }

    public void SetState(DoorState newState)
    {
        // Stop auto-lock timer when state changes; states decide whether to restart it.
        StopAutoLock();

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void SetAnimatorBool(bool isOpen)
    {
        animator.SetBool("IsOpen", isOpen);
    }

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

    public void ShowPromptForCurrentState()
    {
        if (currentState is DoorLockedState)
        {
            ShowPromptForSide(GetLockedText());
        }
        else if (currentState is DoorClosedState)
        {
            ShowPromptForSide(GetInteractText());
        }
        else
        {
            // other states don't show an interaction prompt
            HidePrompts();
        }
    }

    // Lock / Unlock API used by states or external systems (e.g. hack success)
    public void Lock()
    {
        if (isLocked) return;
        isLocked = true;
        pendingLockAfterClose = false; // consume any pending flag
        SetState(new DoorLockedState(this));
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;
        SetState(new DoorOpeningState(this));
    }

    // Called by the hack system (or DoorLockedState.Interact simulation) to indicate hack success
    public void OnHackSuccess()
    {

        Unlock();
    }

    // --- Auto-lock (primary behavior). Replaced the old auto-close-centric logic.
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
        //pendingLockAfterClose = false;
    }

    private IEnumerator AutoLockCoroutine()
    {
        yield return new WaitForSeconds(lockTimeout);

        if (isLocked)
        {
            autoLockCoroutine = null;
            yield break;
        }

        // If door is open, schedule a lock after it closes and initiate closing now.
        if (currentState is DoorOpenState)
        {
            pendingLockAfterClose = true;
            // trigger closing; DoorClosingState -> DoorClosedState.Enter will consume pending flag
            SetState(new DoorClosingState(this));
            Debug.Log("Auto-lock: started closing and scheduled lock after close.");
        }
        // If door is already closed, lock immediately.
        else if (currentState is DoorClosedState)
        {
            SetState(new DoorLockedState(this));
            isLocked = true;
            Debug.Log("Auto-lock: door locked immediately.");
        }
        else
        {
            // For other transient states (opening/closing), just schedule a lock after we reach ClosedState.
            pendingLockAfterClose = true;
            Debug.LogWarning("Auto-lock: else, not good.");
        }

        //Debug.Log("Door has auto-locked due to inactivity (pendingLockAfterClose=" + pendingLockAfterClose + ").");
        autoLockCoroutine = null;
    }

    // Called by DoorClosedState when it enters; returns true if a pending auto-lock was scheduled and consumes it.
    public bool ConsumePendingLock()
    {
        if (!pendingLockAfterClose) return false;
        pendingLockAfterClose = false;
        return true;
    }

    // add this method to DoorInteractable (near ConsumePendingLock or public API)
    public void CancelPendingLock()
    {
        pendingLockAfterClose = false;
    }

    public void HidePrompts()
    {
        Debug.Log("Hiding door prompts.");
        promptFront.Hide();
        promptBack.Hide();
    }
}
