using UnityEngine;

/// <summary>
/// Shows/hides hack mode UI elements.
/// </summary>
public class HackModeUI : MonoBehaviour
{
    [Header("UI Elements to Toggle")]
    [SerializeField] private GameObject[] hackModeObjects;

    private void Start()
    {
        if (PlayerModeController.Instance != null)
        {
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;
            OnModeChanged(PlayerModeController.Instance.CurrentMode);
        } else
        {
            Debug.LogError("[HackModeUI] PlayerModeController instance not found!");
        }
    }

    private void OnDisable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;
    }

    private void OnDestroy()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;
    }

    private void OnModeChanged(PlayerMode mode)
    {
        bool isHackMode = mode == PlayerMode.Hack;

        foreach (var obj in hackModeObjects)
        {
            if (obj != null)
                obj.SetActive(isHackMode);
        }
    }
}