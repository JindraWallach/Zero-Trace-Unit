using System.Collections;
using UnityEngine;

/// <summary>
/// Controller for door interactions using Resolver pattern.
/// Continuously updates prompts while player is in range (coroutine).
/// Event-driven for mode/lock changes + continuous checking.
/// Hides prompts during animations, shows when animation completes.
/// Now uses InteractionResult with colors from ScriptableObject.
/// </summary>
public class DoorInteractionMode : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorStateMachine stateMachine;
    [SerializeField] private HackableDoor hackableDoor;
    [SerializeField] private DoorController doorController;
    [SerializeField] private Transform pivot;

    [Header("Config")]
    [SerializeField] private DoorInteractionConfig config;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.2f; // Update prompts 5x per second

    [Header("Alarm System (Optional)")]
    [SerializeField] private SecurityAlarmSystem alarmSystem;


    private Transform player;
    private bool isPlayerInRange;
    private InteractionResult currentResult;
    private Coroutine updateCoroutine;
    private bool isAnimating;

    private void OnEnable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;

        if (stateMachine != null)
        {
            if (stateMachine.Lock != null)
                stateMachine.Lock.OnLockStateChanged += OnLockStateChanged;

            stateMachine.OnAnimationStarted += OnAnimationStarted;
            stateMachine.OnAnimationCompleted += OnAnimationCompleted;
        }
    }

    private void OnDisable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;

        if (stateMachine != null)
        {
            if (stateMachine.Lock != null)
                stateMachine.Lock.OnLockStateChanged -= OnLockStateChanged;

            stateMachine.OnAnimationStarted -= OnAnimationStarted;
            stateMachine.OnAnimationCompleted -= OnAnimationCompleted;
        }

        StopUpdating();
    }

    public void SetPlayerInRange(Transform playerTransform, bool inRange)
    {
        player = playerTransform;
        isPlayerInRange = inRange;

        doorController.SetPlayerInRange(playerTransform, inRange);

        if (inRange)
            StartUpdating();
        else
            StopUpdating();
    }

    private void OnModeChanged(PlayerMode mode)
    {
        if (isPlayerInRange)
            UpdateInteraction();
    }

    private void OnLockStateChanged(bool isLocked)
    {
        if (isPlayerInRange)
            UpdateInteraction();
    }

    private void OnAnimationStarted()
    {
        isAnimating = true;
        doorController.HidePrompts();
    }

    private void OnAnimationCompleted()
    {
        isAnimating = false;

        if (isPlayerInRange)
            UpdateInteraction();
    }

    private void StartUpdating()
    {
        StopUpdating();
        updateCoroutine = StartCoroutine(UpdateCoroutine());
    }

    private void StopUpdating()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }

        doorController.HidePrompts();
        currentResult = InteractionResult.NoPrompt();
    }

    private IEnumerator UpdateCoroutine()
    {
        var wait = new WaitForSeconds(updateInterval);

        while (isPlayerInRange && player != null)
        {
            UpdateInteraction();
            yield return wait;
        }
    }

    private void UpdateInteraction()
    {
        if (player == null || pivot == null)
            return;

        // Don't show prompts during animation
        if (isAnimating)
        {
            doorController.HidePrompts();
            return;
        }

        float distance = Vector3.Distance(pivot.position, player.position);
        PlayerMode mode = PlayerModeController.Instance.CurrentMode;
        bool isLocked = stateMachine.Lock.IsLocked;
        bool isOpen = IsOpen();

        // Resolve using pure function (now returns formatted text + color)
        currentResult = DoorInteractionResolver.Resolve(mode, isLocked, isOpen, distance, config);

        // Update UI with color-coded prompt
        doorController.SetPrompt(currentResult);
    }

    private bool IsOpen()
    {
        return stateMachine.CurrentState is DoorOpenState;
    }

    public void ExecuteInteraction()
    {
        if (!currentResult.CanInteract)
            return;

        doorController.HidePrompts();

        switch (currentResult.Type)
        {
            case InteractionType.Physical:
                stateMachine.OnInteract();
                break;

            case InteractionType.Hack:
                RequestHack();
                break;
        }
    }

    private void RequestHack()
    {
        hackableDoor.RequestHack(
            onSuccess: () =>
            {
                stateMachine.Lock.Unlock();

                if (stateMachine.Lock.OpenAfterUnlock)
                {
                    stateMachine.SetState(new DoorOpeningState(stateMachine));
                }
                else
                {
                    if (isPlayerInRange)
                        UpdateInteraction();
                }
            },
            onFail: () =>
            {
                // TRIGGER ALARM ON HACK FAILURE
                if (alarmSystem != null)
                {
                    alarmSystem.TriggerAlarm(pivot.position);
                    Debug.Log($"[DoorInteraction] Hack failed - alarm triggered at {pivot.position}", this);
                }
                else
                {
                    // Auto-find alarm system if not assigned
                    alarmSystem = FindFirstObjectByType<SecurityAlarmSystem>();
                    if (alarmSystem != null)
                    {
                        alarmSystem.TriggerAlarm(pivot.position);
                    }
                    else
                    {
                        Debug.LogError("[DoorInteraction] No SecurityAlarmSystem found in scene!", this);
                    }
                }

                if (isPlayerInRange)
                    UpdateInteraction();
            },
            onCancel: () =>
            {
            // Player cancelled - NO ALARM
            if (isPlayerInRange)
                UpdateInteraction();

                Debug.Log("[DoorInteraction] Hack cancelled by player - no alarm", this);
            }
        );
    }
}