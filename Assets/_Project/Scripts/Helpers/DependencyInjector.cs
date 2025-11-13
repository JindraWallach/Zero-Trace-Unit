using Synty.AnimationBaseLocomotion.Samples;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

/// <summary>
/// Central dependency provider. Injects services into IInitializable components.
/// </summary>
public class DependencyInjector : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private SampleCameraController cameraController;

    [Header("Player Components")]
    [SerializeField] private Transform playerPosition;
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Objects to Initialize")]
    [SerializeField] private GameObject[] objectsToInitialize;

    public InputReader InputReader => inputReader;
    public SampleCameraController CameraController => cameraController;
    public Transform PlayerPosition => playerPosition;

    private void Awake()
    {
        // Init all registered objects
        foreach (var go in objectsToInitialize)
        {
            if (go == null) continue;

            var initializables = go.GetComponents<IInitializable>();
            foreach (var init in initializables)
            {
                init.Initialize(this);
            }
        }
    }
}