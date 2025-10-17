using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections;
using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [SerializeField] private Animator animator;

    public float offset = 1.25f;

    private bool isOpen;
    private bool isChanging;
    private string booleanAnimName = "IsOpen";

    public void Initialize(InputReader reader, InteractionPromptUI ui)
    {
        promptUI = ui; 
        reader.onInteract += Interact;
    }

    public override void OnEnterRange()
    {
        promptUI.Show(isOpen ? "Close Door" : "Open Door");
    }

    public override void OnExitRange()
    {
        promptUI.Hide();
    }

    public override void Interact()
    {
        if (isChanging) return;

        isOpen = !isOpen;
        animator.SetBool(booleanAnimName, isOpen);
        promptUI.Show(isOpen ? "Close Door" : "Open Door");
        Debug.Log(isOpen ? "Door opened" : "Door closed");


        StartCoroutine(InteractionCooldown());
    }

    private IEnumerator InteractionCooldown()
    {
        isChanging = true;
        yield return new WaitForSeconds(offset);
        isChanging = false;
    }
}
