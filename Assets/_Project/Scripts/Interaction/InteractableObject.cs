using UnityEngine;

[RequireComponent(typeof(InteractionPromptUI))]
public abstract class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "Use";
    protected InteractionPromptUI promptUI;

    protected virtual void Awake()
    {
        promptUI = GetComponent<InteractionPromptUI>();
    }

    public virtual string GetInteractText() => interactText;

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
