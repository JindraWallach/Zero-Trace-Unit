using UnityEngine;

/// <summary>
/// Controls puzzle overlay UI (progress, timer, hints).
/// Used by PuzzleBase descendants.
/// </summary>
public class PuzzleUIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform container;

    public RectTransform Container => container;

    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}
