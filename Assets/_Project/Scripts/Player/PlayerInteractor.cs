using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player interaction detection via trigger collider.
/// Manages IInteractable targets in range.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask interactableLayers;

    private readonly List<IInteractable> interactablesInRange = new();
    private IInteractable currentTarget;
    private InputReader inputReader;

    public void Initialize(InputReader reader)
    {
        inputReader = reader;
        inputReader.onInteract += TryInteract;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.onInteract -= TryInteract;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, interactableLayers)) return;
        if (!other.TryGetComponent(out IInteractable interactable)) return;

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

        interactable.OnExitRange();
        interactablesInRange.Remove(interactable);
        UpdateCurrentTarget();
    }

    private void UpdateCurrentTarget()
    {
        interactablesInRange.RemoveAll(i => i == null);

        if (interactablesInRange.Count == 0)
        {
            if (currentTarget != null)
            {
                HidePromptFor(currentTarget);
                currentTarget = null;
            }
            return;
        }

        var nearest = FindNearest();
        if (nearest != currentTarget)
        {
            if (currentTarget != null)
                HidePromptFor(currentTarget);

            currentTarget = nearest;

            if (currentTarget != null)
                ShowPromptFor(currentTarget);
        }
    }

    private IInteractable FindNearest()
    {
        float minDist = float.MaxValue;
        IInteractable nearest = null;

        foreach (var i in interactablesInRange)
        {
            if (i is not MonoBehaviour mb) continue;

            float dist = Vector3.Distance(transform.position, mb.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

    private void ShowPromptFor(IInteractable target)
    {
        if (target is InteractableBase ib) {
            ib.ShowPromptForPlayer(transform);
        }
    }

    private void HidePromptFor(IInteractable target)
    {
        if (target is InteractableBase ib)
            ib.HidePromptForPlayer();
    }

    private void TryInteract()
    {
        currentTarget?.Interact();
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask & (1 << layer)) != 0;
    }
}