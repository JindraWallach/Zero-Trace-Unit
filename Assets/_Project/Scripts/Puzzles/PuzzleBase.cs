using System;
using UnityEngine;

/// <summary>
/// Abstract base for all puzzles.
/// Handles callbacks, input blocking, and lifecycle.
/// </summary>
public abstract class PuzzleBase : MonoBehaviour
{
    public event Action OnSuccess;
    public event Action OnFail;
    public event Action OnCancel;

    protected PuzzleUIController uiController;
    protected bool isActive;

    protected virtual void Awake()
    {
        uiController = GetComponent<PuzzleUIController>();
    }

    public virtual void StartPuzzle()
    {
        isActive = true;
        uiController?.Show();
        OnPuzzleStart();
        Debug.Log($"[{GetType().Name}] Puzzle started");
    }

    public virtual void CompletePuzzle()
    {
        if (!isActive) return;

        isActive = false;
        OnPuzzleComplete();
        OnSuccess?.Invoke();
        Debug.Log($"[{GetType().Name}] Puzzle completed");
    }

    public virtual void FailPuzzle()
    {
        if (!isActive) return;

        isActive = false;
        OnPuzzleFail();
        OnFail?.Invoke();
        Debug.Log($"[{GetType().Name}] Puzzle failed");
    }

    public virtual void CancelPuzzle()
    {
        Debug.Log("CancelPuzzle called");
        if (!isActive) return;
        Debug.Log("Puzzle is active, proceeding to cancel");

        isActive = false;
        OnPuzzleCancel();
        OnCancel?.Invoke();
        Debug.Log($"[{GetType().Name}] Puzzle cancelled");
    }

    // Override these in descendants
    protected abstract void OnPuzzleStart();
    protected virtual void OnPuzzleComplete() { }
    protected virtual void OnPuzzleFail() { }
    protected virtual void OnPuzzleCancel() { }
}