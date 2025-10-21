using UnityEngine;

[RequireComponent(typeof(InteractionPromptUI))]
public abstract class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] protected string interactText = "Use";
    [SerializeField] protected string lockedText = "HACK";
    protected InteractionPromptUI promptUI;

    protected virtual void Awake()
    {
        promptUI = GetComponent<InteractionPromptUI>();
    }

    public virtual string GetInteractText() => interactText;

    public virtual string GetLockedText() => lockedText;

    public abstract void Interact();

    public virtual void OnEnterRange()
    {
        promptUI.Show(interactText);
    }

    public virtual void OnExitRange()
    {
        promptUI.Hide();
    }
}
