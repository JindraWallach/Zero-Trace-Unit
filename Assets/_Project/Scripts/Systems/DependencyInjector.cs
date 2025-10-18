using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

public class DependencyInjector : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private InputReader inputReader;

    [Header("Player Components")]
    [SerializeField] private PlayerInteractionDetector interactionDetector;

    [Header("Interactable Objects")]
    [SerializeField] private DoorInteractable[] doors;

    private void Awake()
    {
        // Inicializace hráče
        interactionDetector.Initialize(inputReader);

        // Inicializace dveří
        foreach (var door in doors)
        {
            door.Initialize(inputReader);
        }
    }
}
