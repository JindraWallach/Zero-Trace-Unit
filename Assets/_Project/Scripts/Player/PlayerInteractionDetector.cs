using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class PlayerInteractionDetector : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private float detectionRadius = 2.5f;

    [Header("References")] 
    [SerializeField] private InteractionPromptUI promptUI;
    [SerializeField] private LayerMask interactableLayers;

    private readonly List<IInteractable> interactablesInRange = new();
    private IInteractable currentTarget;

    private void Awake()
    {
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = detectionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnEnterRange();
            interactablesInRange.Add(interactable);
            UpdateCurrentTarget();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnExitRange();
            interactablesInRange.Remove(interactable);
            UpdateCurrentTarget();
        }
    }

    private void UpdateCurrentTarget()
    {
        if (interactablesInRange.Count > 0)
        {
            currentTarget = interactablesInRange[^1]; // poslední vstoupený
            //promptUI.SetUp(currentTarget.GetInteractText()); TODO: implementovat text
        }
        else
        {
            currentTarget = null;
            promptUI.Hide();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed || currentTarget == null)
            return;

        currentTarget.Interact();
    }
}
