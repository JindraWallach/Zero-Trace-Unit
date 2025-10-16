using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

public class DependencyInjector : MonoBehaviour
{
    [SerializeField] private InputReader inputReader;
    [SerializeField] private PlayerInteractionDetector interactionDetector;

    private void Awake()
    {
        interactionDetector.Initialize(inputReader);
        // více zavislostí sem
    }
}
