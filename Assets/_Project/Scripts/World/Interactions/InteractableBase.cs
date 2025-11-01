// Scripts/World/Interactions/InteractableBase.cs
using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected string interactText = "Use";
    [SerializeField] protected string lockedText = "HACK";

    protected bool isInRange;

    public virtual string GetInteractText() => interactText;
    public virtual string GetLockedText() => lockedText;

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