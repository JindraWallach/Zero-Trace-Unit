using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LayerMask interactableLayers;

    private readonly List<IInteractable> interactablesInRange = new();
    private IInteractable currentTarget;
    private InputReader inputReader;

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.onInteract -= TryInteract;
    }

    public void Initialize(InputReader reader)
    {
        inputReader = reader;
        inputReader.onInteract += TryInteract;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, interactableLayers)) return;
        if (!other.TryGetComponent(out IInteractable interactable)) return;

        // notify object it's in range (used for logic like isActive) but do NOT show its UI here
        interactable.OnEnterRange();

        if (!interactablesInRange.Contains(interactable))
        {
            interactablesInRange.Add(interactable);
            UpdateCurrentTarget();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out IInteractable interactable)) return;

        // notify object left range (also ensures prompt hidden)
        interactable.OnExitRange();
        interactablesInRange.Remove(interactable);
        UpdateCurrentTarget();
    }

    private void UpdateCurrentTarget()
    {
        // remove destroyed/null entries
        interactablesInRange.RemoveAll(i => i == null);

        if (interactablesInRange.Count == 0)
        {
            // hide prompt on previous target
            if (currentTarget != null)
            {
                if (currentTarget is InteractableObject ioPrev)
                    ioPrev.HidePromptForPlayer();
                else
                    currentTarget.OnExitRange();

                currentTarget = null;
            }
            return;
        }

        // find nearest
        float minDist = float.MaxValue;
        IInteractable nearest = null;

        foreach (var i in interactablesInRange)
        {
            var mb = i as MonoBehaviour;
            if (mb == null) continue;

            float dist = Vector3.Distance(transform.position, mb.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        // if nearest changed, update prompts
        if (nearest != currentTarget)
        {
            // hide previous
            if (currentTarget != null)
            {
                if (currentTarget is InteractableObject ioPrev)
                    ioPrev.HidePromptForPlayer();
                else
                    currentTarget.OnExitRange();
            }

            currentTarget = nearest;

            // show prompt for new nearest
            if (currentTarget != null)
            {
                if (currentTarget is InteractableObject ioNew)
                    ioNew.ShowPromptForPlayer(transform);
                else
                    currentTarget.OnEnterRange();
            }
        }
    }

    private void TryInteract()
    {
        Debug.Log($"Trying to interact with current target {currentTarget}");
        currentTarget?.Interact();
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask & (1 << layer)) != 0;
    }
}
