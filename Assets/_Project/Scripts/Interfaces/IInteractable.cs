using UnityEngine;

public interface IInteractable
{
    string GetInteractText();
    void Interact(GameObject player);
    void OnEnterRange(GameObject player);
    void OnExitRange(GameObject player);
}