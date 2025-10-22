using UnityEngine;

[RequireComponent(typeof(InteractionPromptUI))]
public abstract class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] protected string interactText = "Use";
    [SerializeField] protected string lockedText = "HACK";
    protected InteractionPromptUI promptUI;

    // track whether this object is currently in range of a player (set by detector)
    protected bool isInRange;

    protected virtual void Awake()
    {
        promptUI = GetComponent<InteractionPromptUI>();
    }

    public virtual string GetInteractText() => interactText;

    public virtual string GetLockedText() => lockedText;

    public abstract void Interact();

    // keep these so objects can react to being in range, but DO NOT show UI here anymore
    public virtual void OnEnterRange()
    {
        isInRange = true;
    }

    public virtual void OnExitRange()
    {
        isInRange = false;
        // ensure prompt hidden when truly leaving range
        HidePromptForPlayer();
    }

    // New API: show/hide prompt for a specific player (player Transform provided
    // so interactables like DoorInteractable can choose front/back)
    public virtual void ShowPromptForPlayer(Transform player)
    {
        if (promptUI != null)
            promptUI.Show(GetInteractText());
    }

    public virtual void HidePromptForPlayer()
    {
        if (promptUI != null)
            promptUI.Hide();
    }
}
