using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [SerializeField] private Animator animator;

    public void Initialize(InputReader reader, InteractionPromptUI ui)
    {
        promptUI = ui; 
        reader.onInteract += Interact;
    }

    public override void OnEnterRange()
    {
        promptUI.Show("Open Door");
    }

    public override void OnExitRange()
    {
        promptUI.Hide();
    }

    public override void Interact()
    {
        //animator.SetTrigger("Open");
        //TODO: make an animation

        Debug.Log("Door opened");
    }
}
