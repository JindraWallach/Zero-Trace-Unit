using UnityEngine;

/// <summary>
/// Automatically opens door when enemy approaches.
/// Respects security levels and lock state.
/// Only works if enemy has clearance.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class DoorEnemyAutoOpen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorStateMachine doorMachine;

    [Header("Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private SphereCollider triggerCollider;

    private void Awake()
    {
        // Setup trigger collider
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = detectionRadius;

        if (doorMachine == null)
        {
            doorMachine = GetComponentInParent<DoorStateMachine>();
        }

        if (doorMachine == null)
        {
            Debug.LogError("[DoorEnemyAutoOpen] Missing DoorStateMachine reference!", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        // Early exit: not an enemy
        if (((1 << other.gameObject.layer) & enemyLayer) == 0)
            return;

        // Early exit: door not closed
        //if (!(doorMachine.CurrentState is DoorClosedState))
        //    return;

        // Check security clearance
        if (!doorMachine.Lock.CanEnemyOpen())
        {
            if (showDebugLogs)
            {
                Debug.Log($"[DoorEnemyAutoOpen] Enemy {other.name} denied access " +
                         $"(Level: {doorMachine.Lock.SecurityLevel})", this);
            }
            return;
        }

        // Open door
        if (showDebugLogs)
        {
            Debug.Log($"[DoorEnemyAutoOpen] Auto-opening door for enemy: {other.name} " +
                     $"(Level: {doorMachine.Lock.SecurityLevel})", this);
        }

        doorMachine.SetState(new DoorOpeningState(doorMachine));
    }
}