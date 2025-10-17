using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections;
using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [SerializeField] private Animator animator;

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

        isChanging = true;
        StartCoroutine(ChangeStateWithDelay(() =>
        {
            isOpen = !isOpen;
            animator.SetBool(booleanAnimName, isOpen);
            promptUI.Show(isOpen ? "Close Door" : "Open Door");

            Debug.Log(isOpen ? "Door opened" : "Door closed");
            isChanging = false;
        }));
    }

    private IEnumerator ChangeStateWithDelay(System.Action onComplete)
    {
        yield return new WaitForSeconds(0.25f);
        onComplete?.Invoke(); //callbackujeme () => po start courotine
    }
}
