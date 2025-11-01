using UnityEngine;

/// <summary>
/// Controls visual hack mode overlay (post-processing, camera effects).
/// Activated by HackManager during puzzle sessions.
/// </summary>
public class HackModeController : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject hackModePostProcess;
    [Tooltip("IF needed 2nd Camera")]
    [SerializeField] private Camera hackCamera;

    private bool isActive;

    public void EnableHackMode()
    {
        isActive = true;
        if (hackModePostProcess != null)
            hackModePostProcess.SetActive(true);

        Debug.Log("[HackModeController] Hack mode enabled");
    }

    public void DisableHackMode()
    {
        isActive = false;
        if (hackModePostProcess != null)
            hackModePostProcess.SetActive(false);

        Debug.Log("[HackModeController] Hack mode disabled");
    }

    public bool IsActive => isActive;
}