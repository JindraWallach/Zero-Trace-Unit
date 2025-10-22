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

    private void Awake()
    {
        var sc = GetComponent<SphereCollider>();
        if (sc == null)
            Debug.LogError("PlayerInteractionDetector: Missing SphereCollider (RequireComponent should ensure it).");
        else if (!sc.isTrigger)
            Debug.LogWarning("PlayerInteractionDetector: SphereCollider.isTrigger is false. Trigger events require a trigger collider. Set isTrigger = true in the inspector.");

        if (GetComponent<Rigidbody>() == null)
            Debug.LogWarning("PlayerInteractionDetector: No Rigidbody found on this GameObject. For trigger events at least one collider in the pair must have a Rigidbody (kinematic is OK).");
    }

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
        Debug.Log($"OnTriggerEnter: {other.gameObject.name} (layer {other.gameObject.layer})");
        if (!IsInLayerMask(other.gameObject.layer, interactableLayers))
        {
            Debug.LogWarning($"OnTriggerEnter: {other.gameObject.name} is not in interactableLayers.");
            return;
        }

        if (!other.TryGetComponent(out IInteractable interactable))
        {
            Debug.LogWarning($"OnTriggerEnter: {other.gameObject.name} does not implement IInteractable.");
            return;
        }

        if (!interactablesInRange.Contains(interactable))
        {
            interactable.OnEnterRange();
            interactablesInRange.Add(interactable);
            UpdateCurrentTarget();
        }
        else
        {
            Debug.LogWarning($"OnTriggerEnter: {other.gameObject.name} already in range list.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"OnTriggerEnter: {other.name} layer = {LayerMask.LayerToName(other.gameObject.layer)}");

        if (!other.TryGetComponent(out IInteractable interactable))
        {
            Debug.Log($"OnTriggerExit: {other.gameObject.name} does not implement IInteractable.");
            return;
        }

        interactable.OnExitRange();
        interactablesInRange.Remove(interactable);
        UpdateCurrentTarget();
    }

    private void UpdateCurrentTarget()
    {
        // remove destroyed/null entries
        interactablesInRange.RemoveAll(i => i == null);

        Debug.Log($"Updating current interactable target. Count = {interactablesInRange.Count}");

        if (interactablesInRange.Count == 0)
        {
            currentTarget = null;
            Debug.Log("No interactables in range -> currentTarget cleared.");
            return;
        }

        float minDist = float.MaxValue;
        IInteractable nearest = null;

        foreach (var i in interactablesInRange)
        {
            var mb = i as MonoBehaviour;
            if (mb == null) continue; // safety

            float dist = Vector3.Distance(transform.position, mb.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        currentTarget = nearest;
        Debug.Log($"Selected currentTarget = {(nearest as MonoBehaviour)?.gameObject.name ?? "null"} at distance {minDist}");
    }

    private void TryInteract()
    {
        Debug.Log($"Trying to interact with current target {currentTarget}");
        if (currentTarget == null)
        {
            Debug.Log("TryInteract: currentTarget is null. No interaction performed.");
            return;
        }

        currentTarget.Interact();
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask & (1 << layer)) != 0;
    }
}
