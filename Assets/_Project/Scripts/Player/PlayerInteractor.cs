using UnityEngine;
using System.Collections.Generic;

public class PlayerInteractor : MonoBehaviour, IInitializable
{
    [Header("Detection")]
    public LayerMask interactableLayerMask = -1;
    public float maxInteractDistance = 3f;

    private List<IInteractable> inRangeTargets = new List<IInteractable>();
    private DependencyInjector di;

    public void Initialize(DependencyInjector dependencyInjector)
    {
        di = dependencyInjector;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsOnLayer(other.gameObject.layer))
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null && !inRangeTargets.Contains(interactable))
            {
                inRangeTargets.Add(interactable);
                interactable.OnEnterRange(gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsOnLayer(other.gameObject.layer))
        {
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null && inRangeTargets.Contains(interactable))
            {
                inRangeTargets.Remove(interactable);
                interactable.OnExitRange(gameObject);
            }
        }
    }

    public void TryInteract()
    {
        var target = GetNearestTarget();
        if (target != null)
        {
            target.Interact(gameObject);
        }
    }

    private IInteractable GetNearestTarget()
    {
        if (inRangeTargets.Count == 0) return null;

        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var target in inRangeTargets)
        {
            var targetTransform = (target as Component)?.transform;
            if (targetTransform == null) continue;

            float distance = Vector3.Distance(transform.position, targetTransform.position);
            if (distance < nearestDistance && distance <= maxInteractDistance)
            {
                nearestDistance = distance;
                nearest = target;
            }
        }

        return nearest;
    }

    private bool IsOnLayer(int layer)
    {
        return (interactableLayerMask.value & (1 << layer)) != 0;
    }
}