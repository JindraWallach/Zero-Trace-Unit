// Scripts/World/Interactions/InteractableBase.cs
using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{

    protected bool isInRange;

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