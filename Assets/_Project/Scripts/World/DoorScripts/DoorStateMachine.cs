using UnityEngine;

/// <summary>
/// State machine for door states: Locked, Closed, Opening, Open, Closing.
/// Delegates animation to DoorController.
/// </summary>
public class DoorStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorController doorController;
    [SerializeField] private LockSystem lockSystem;

    [Header("Debug")]
    [SerializeField] private string currentStateName;

    private DoorState currentState;
    private Transform player;

    private void Start()
    {
        if (lockSystem.IsLocked)
            SetState(new DoorLockedState(this));
        else
            SetState(new DoorClosedState(this));
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void SetState(DoorState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentStateName = currentState?.GetType().Name ?? "None";
        currentState.Enter();
    }

    public void OnInteract()
    {
        currentState?.Interact();
    }

    // === Public API for states ===
    public DoorController Controller => doorController;
    public LockSystem Lock => lockSystem;
    public float AnimDuration => doorController.AnimationDuration;

    public void SetPlayerReference(Transform playerTransform)
    {
        player = playerTransform;
    }

    public Transform Player => player;
}