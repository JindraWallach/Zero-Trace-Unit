// Scripts/Mission/MissionUIHandler.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SRP: Listens to MissionManager.OnMissionCompleted and handles ONLY UI overlay.
///
/// Time.timeScale je VLASTNĚN GameManagerem.
/// Voláme GameManager.OnMissionComplete() (viz GameManager_PATCH.cs).
/// Nikdy nepíšeme Time.timeScale přímo.
///
/// Event-driven: no Update(), no polling.
/// </summary>
[DisallowMultipleComponent]
public class MissionUIHandler : MonoBehaviour
{
    [Header("Mission Complete UI")]
    [Tooltip("Canvas nebo panel zobrazený po dokončení mise")]
    [SerializeField] private GameObject missionCompleteOverlay;

    [Tooltip("Volitelný Text element – nech prázdné pokud nepotřebuješ")]
    [SerializeField] private TextMeshProUGUI missionCompleteText;

    [Header("Settings")]
    [SerializeField] private string completeMessage = "MISSION COMPLETE";

    // ───────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (missionCompleteOverlay != null)
            missionCompleteOverlay.SetActive(false);
    }

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
        else
            StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
    }

    // ── Event handler ──────────────────────────────────────────────────────

    private void HandleMissionCompleted()
    {
        Debug.Log("[MissionUIHandler] Showing MISSION COMPLETE.");

        if (missionCompleteOverlay != null)
            missionCompleteOverlay.SetActive(true);

        if (missionCompleteText != null)
            missionCompleteText.text = completeMessage;

        // Time.timeScale vlastní GameManager – voláme jeho API, nikdy nepíšeme přímo.
        // GameManager.OnMissionComplete() přidáš dle GameManager_PATCH.cs
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMissionComplete();
        }
        else
        {
            Debug.LogWarning("[MissionUIHandler] GameManager not found – falling back to direct timeScale.");
            Time.timeScale = 0f;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        yield return null;
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
        else
            Debug.LogError("[MissionUIHandler] MissionManager not found after one frame!");
    }
}