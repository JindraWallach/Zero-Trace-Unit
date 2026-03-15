// Scripts/Mission/MissionUIHandler.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SRP: Reaguje na MissionManager.OnMissionCompleted – zobrazí overlay.
/// Text a barvu čte z MissionSystemConfig SO.
/// Time.timeScale vlastní GameManager – voláme GameManager.OnMissionComplete().
/// Event-driven: no Update(), no polling.
/// </summary>
[DisallowMultipleComponent]
public class MissionUIHandler : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MissionSystemConfig config;

    [Header("UI References")]
    [SerializeField] private GameObject missionCompleteOverlay;
    [SerializeField] private TextMeshProUGUI missionCompleteText;

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

    private void HandleMissionCompleted()
    {
        if (missionCompleteOverlay != null)
            missionCompleteOverlay.SetActive(true);

        if (missionCompleteText != null && config != null)
        {
            missionCompleteText.text = config.missionCompleteText;
            missionCompleteText.color = config.missionCompleteColor;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnMissionComplete();
        else
        {
            Debug.LogWarning("[MissionUIHandler] GameManager not found – fallback timeScale.");
            Time.timeScale = 0f;
        }
    }

    private System.Collections.IEnumerator SubscribeWhenReady()
    {
        yield return null;
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
        else
            Debug.LogError("[MissionUIHandler] MissionManager not found after one frame!");
    }
}