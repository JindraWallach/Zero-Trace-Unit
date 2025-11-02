using System.Collections;
using UnityEngine;

/// <summary>
/// Manages door lock state and auto-lock timer.
/// </summary>
public class LockSystem : MonoBehaviour
{
    [Header("Lock Settings")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private bool enableAutoLock = true;
    [SerializeField] private bool enableAutoClose = true;
    [SerializeField] private float autoLockDelay = 30f;
    [SerializeField] private float autoCloseDelay = 30f;

    [Header("Debug")]
    [SerializeField] private bool isLockedDebug;

    private bool isLocked;
    private Coroutine autoLockCoroutine;
    private DoorStateMachine stateMachine;

    public bool IsLocked => isLocked;
    public bool EnableAutoClose => enableAutoClose;
    public float AutoCloseDelay => autoCloseDelay;

    private void Awake()
    {
        isLocked = startLocked;
        isLockedDebug = isLocked;
        stateMachine = GetComponent<DoorStateMachine>();
    }

    public void Lock()
    {
        if (isLocked) return;

        isLocked = true;
        isLockedDebug = true;
        StopAutoLock();
        stateMachine.SetState(new DoorLockedState(stateMachine));
        Debug.Log("[LockSystem] Door locked");
    }

    public void Unlock()
    {
        if (!isLocked) return;

        isLocked = false;
        isLockedDebug = false;
        Debug.Log("[LockSystem] Door unlocked");
    }

    public void StartAutoLock()
    {
        if (!enableAutoLock) return;

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