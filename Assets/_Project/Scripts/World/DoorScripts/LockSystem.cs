using System.Collections;
using UnityEngine;

/// <summary>
/// Manages door lock state and auto-lock timer.
/// </summary>
public class LockSystem : MonoBehaviour
{
    [Header("Lock Settings")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private float autoLockDelay = 30f;

    private bool isLocked;
    private Coroutine autoLockCoroutine;
    private DoorStateMachine stateMachine;

    public bool IsLocked => isLocked;

    private void Awake()
    {
        isLocked = startLocked;
        stateMachine = GetComponent<DoorStateMachine>();
    }

    public void Lock()
    {
        if (isLocked) return;

        isLocked = true;
        StopAutoLock();
        stateMachine.SetState(new DoorLockedState(stateMachine));
        Debug.Log("[LockSystem] Door locked");
    }

    public void Unlock()
    {
        if (!isLocked) return;

        isLocked = false;
        Debug.Log("[LockSystem] Door unlocked");
    }

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
        yield return new WaitForSeconds(autoLockDelay);

        if (!isLocked)
        {
            Lock();
            Debug.Log("[LockSystem] Auto-lock triggered");
        }

        autoLockCoroutine = null;
    }
}