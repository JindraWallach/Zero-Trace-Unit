using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sci-fi hack mode overlay (lines to targets, icons, stats).
/// Watch Dogs 2 style visual layer.
/// </summary>
public class HackOverlayUI : MonoBehaviour
{
    [Header("Visual Elements")]
    [SerializeField] private Image[] scanLines;
    [SerializeField] private CanvasGroup canvasGroup;

    private bool isActive;

    public void Show()
    {
        isActive = true;
        gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Debug.Log("[HackOverlayUI] Overlay shown");
    }

    public void Hide()
    {
        isActive = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
        Debug.Log("[HackOverlayUI] Overlay hidden");
    }

    // TODO: Draw lines to targets via LineRenderer or UI Image
    public void UpdateTargetLines(Vector3[] targetPositions)
    {
        // Implementation for drawing lines (future feature)
    }
}