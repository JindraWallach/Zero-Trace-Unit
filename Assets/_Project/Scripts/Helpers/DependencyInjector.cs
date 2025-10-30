using UnityEngine;

public class DependencyInjector : MonoBehaviour
{
    [Header("Scene References")]
    public Camera mainCamera;
    public Transform playerTransform;
    public PlayerInteractor playerInteractor;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerTransform == null && playerInteractor != null)
            playerTransform = playerInteractor.transform;
    }

    private void Start()
    {
        var initializables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in initializables)
        {
            if (mb is IInitializable init)
            {
                init.Initialize(this);
            }
        }
    }
}