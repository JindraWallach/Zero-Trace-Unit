using Synty.AnimationBaseLocomotion.Samples;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System.Linq;
using UnityEngine;

public class DependencyInjector : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private SampleCameraController cameraController;

    [Header("Player Components")]
    [SerializeField] private Transform playerPosition;
    [SerializeField] private PlayerInteractor interactionDetector;

    [Header("Objects to Initialize")]
    [SerializeField] private GameObject[] objectsToInitialize;

    public InputReader InputReader => inputReader;
    public SampleCameraController CameraController => cameraController;
    public Transform PlayerPosition => playerPosition;

    private void Awake()
    {
        interactionDetector.Initialize(inputReader);

        foreach (var go in objectsToInitialize)
        {
            var initializables = go.GetComponents<IInitializable>();
            foreach (var init in initializables)
            {
                init.Initialize(this);
            }
        }
    }
}
