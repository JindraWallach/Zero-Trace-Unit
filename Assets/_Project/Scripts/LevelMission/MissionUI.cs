// Scripts/Mission/MissionUI.cs
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// SRP: Mission UI only – HUD popup on objective change + pause menu objective text.
/// Listens to MissionManager.OnObjectiveChanged (static event).
/// No Update(). Fade via coroutine with unscaledDeltaTime (works while paused).
/// </summary>
[DisallowMultipleComponent]
public class MissionUI : MonoBehaviour
{
    [Header("Pause Menu")]
    [Tooltip("TMP text inside the Pause Menu showing current objective")]
    [SerializeField] private TextMeshProUGUI pauseMenuObjectiveText;

    [Header("HUD Popup")]
    [Tooltip("Root panel of the popup (CanvasGroup required or will be added)")]
    [SerializeField] private GameObject popupPanel;

    [Tooltip("TMP text inside the popup")]
    [SerializeField] private TextMeshProUGUI popupObjectiveText;

    [Tooltip("Optional label text above the objective (e.g. 'NEW OBJECTIVE')")]
    [SerializeField] private TextMeshProUGUI popupLabelText;

    [Header("Popup Settings")]
    [Range(0.1f, 1f)]
    [SerializeField] private float fadeDuration = 0.3f;

    // ── Cache ──────────────────────────────────────────────────────────────
    private CanvasGroup _popupCanvasGroup;
    private Coroutine _popupCoroutine;

    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (popupPanel != null)
        {
            _popupCanvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (_popupCanvasGroup == null)
                _popupCanvasGroup = popupPanel.AddComponent<CanvasGroup>();

            popupPanel.SetActive(false);
        }

        if (pauseMenuObjectiveText != null)
            pauseMenuObjectiveText.text = string.Empty;
    }

    private void OnEnable()
    {
        MissionManager.OnObjectiveChanged += HandleObjectiveChanged;
    }

    private void OnDisable()
    {
        MissionManager.OnObjectiveChanged -= HandleObjectiveChanged;
    }

    // ── Event handler ──────────────────────────────────────────────────────

    private void HandleObjectiveChanged(MissionObjectiveSO objective)
    {
        SetPauseMenuText(objective.objectiveText);
        ShowPopup(objective);
    }

    // ── Pause menu ─────────────────────────────────────────────────────────

    private void SetPauseMenuText(string text)
    {
        if (pauseMenuObjectiveText != null)
            pauseMenuObjectiveText.text = text;
    }

    // ── Popup ──────────────────────────────────────────────────────────────

    private void ShowPopup(MissionObjectiveSO objective)
    {
        if (popupPanel == null || _popupCanvasGroup == null) return;

        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);

        _popupCoroutine = StartCoroutine(PopupRoutine(objective));
    }

    private IEnumerator PopupRoutine(MissionObjectiveSO objective)
    {
        if (popupObjectiveText != null)
            popupObjectiveText.text = objective.objectiveText;

        if (popupLabelText != null)
            popupLabelText.text = "NEW OBJECTIVE";

        popupPanel.SetActive(true);
        _popupCanvasGroup.alpha = 0f;

        yield return StartCoroutine(Fade(0f, 1f));
        yield return new WaitForSecondsRealtime(objective.popupDuration);
        yield return StartCoroutine(Fade(1f, 0f));

        popupPanel.SetActive(false);
        _popupCoroutine = null;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        _popupCanvasGroup.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _popupCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        _popupCanvasGroup.alpha = to;
    }
}