using UnityEngine;

/// <summary>
/// Base class for all interactable world objects.
/// Manages prompt UI via UIPromptController.
/// </summary>

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected string interactText = "Use";
    [SerializeField] protected string lockedText = "HACK";
    public virtual string GetInteractText() => interactText;
    public virtual string GetLockedText() => lockedText;

    protected UIPromptController promptController;
    protected bool isInRange;

    protected virtual void Awake()
    {
        promptController = GetComponent<UIPromptController>();
        UIManager.Instance?.RegisterPrompt(promptController);
    }

    protected virtual void OnDestroy()
    {
        UIManager.Instance?.UnregisterPrompt(promptController);
    }

    public abstract void Interact();

    public virtual void OnEnterRange()
    {
        isInRange = true;
    }

    public virtual void OnExitRange()
    {
        isInRange = false;
        HidePromptForPlayer();
    }

    public virtual void ShowPromptForPlayer(Transform player) { }
    public virtual void HidePromptForPlayer() { }
}