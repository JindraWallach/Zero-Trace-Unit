using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PlayerInteractionDetector : MonoBehaviour
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

        Debug.Log($"Entering interaction range of {((MonoBehaviour)interactable).name}");
        interactable.OnEnterRange();
        interactablesInRange.Add(interactable);
        UpdateCurrentTarget();
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
        if (interactablesInRange.Count == 0)
        {
            currentTarget = null;
            return;
        }

        // vezmi nejbližší
        float minDist = float.MaxValue;
        IInteractable nearest = null;

        foreach (var i in interactablesInRange)
        {
            float dist = Vector3.Distance(transform.position, ((MonoBehaviour)i).transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        currentTarget = nearest;
    }

    private void TryInteract()
    {
        currentTarget?.Interact();
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == (mask | (1 << layer));
    }
}
