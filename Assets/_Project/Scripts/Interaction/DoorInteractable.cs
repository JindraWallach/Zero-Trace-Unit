using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [SerializeField] private Animator doorAnimator;
    private bool isOpen;

    public override void Interact()
    {
        isOpen = !isOpen;
        //doorAnimator.SetBool("Open", isOpen);
        Debug.Log($"[Door] {(isOpen ? "Opened" : "Closed")} {gameObject.name}");
    }
}
