using System.Collections;
using UnityEngine;

/// <summary>
/// Controller for door interactions using Resolver pattern.
/// Continuously updates prompts while player is in range (coroutine).
/// Event-driven for mode/lock changes + continuous checking.
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

    private Transform player;
    private bool isPlayerInRange;
    private InteractionResult currentResult;
    private Coroutine updateCoroutine;

    private void OnEnable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;

        if (stateMachine != null && stateMachine.Lock != null)
            stateMachine.Lock.OnLockStateChanged += OnLockStateChanged;
    }

    private void OnDisable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;

        if (stateMachine != null && stateMachine.Lock != null)
            stateMachine.Lock.OnLockStateChanged -= OnLockStateChanged;

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

        float distance = Vector3.Distance(pivot.position, player.position);
        PlayerMode mode = PlayerModeController.Instance.CurrentMode;
        bool isLocked = stateMachine.Lock.IsLocked;

        // Resolve using pure function
        currentResult = DoorInteractionResolver.Resolve(mode, isLocked, distance, config);

        // Update UI based on result
        if (currentResult.ShowPrompt)
            doorController.SetPromptEnabled(true, currentResult.PromptText);
        else
            doorController.SetPromptEnabled(false);
    }

    public void ExecuteInteraction()
    {
        if (!currentResult.CanInteract)
            return;

        switch (currentResult.Type)
        {
            case InteractionType.Physical:
                stateMachine.OnInteract();
                break;

            case InteractionType.Hack:
                hackableDoor.RequestHack(
                    onSuccess: () =>
                    {
                        stateMachine.Lock.Unlock();

                        if (stateMachine.Lock.OpenAfterUnlock)
                            stateMachine.SetState(new DoorOpeningState(stateMachine));
                    },
                    onFail: () => { /* Silent fail */ }
                );
                break;
        }
    }
}