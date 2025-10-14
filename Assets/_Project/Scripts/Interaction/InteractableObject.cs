using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "Interact";

    public string GetInteractText() => interactText;

    public void Interact()
    {
        Debug.Log($"[Interact] {gameObject.name}");
    }

    public void OnEnterRange()
    {
        Debug.Log($"[Range Enter] {gameObject.name}");
    }

    public void OnExitRange()
    {
        Debug.Log($"[Range Exit] {gameObject.name}");
    }
}
