using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space interaction prompt with dynamic text and color.
/// Supports formatted text with input keys and color coding.
/// </summary>
public class UIPromptController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage; // Optional: color background too

    [Header("Animation (Optional)")]
    [SerializeField] private bool enablePulse = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMin = 0.8f;
    [SerializeField] private float pulseMax = 1.0f;

    private Color currentColor = Color.white;
    private float pulseTimer;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Hide();
    }

    private void Update()
    {
        if (enablePulse && canvasGroup != null && canvasGroup.alpha > 0)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);

            if (promptText != null)
            {
                Color pulsedColor = currentColor * scale;
                pulsedColor.a = currentColor.a;
                promptText.color = pulsedColor;
            }
        }
    }

    /// <summary>
    /// Show prompt with text and color.
    /// </summary>
    public void Show(string text, Color color)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false; // Don't block raycasts for world-space UI
        }

        if (promptText != null)
        {
            promptText.text = text;
            currentColor = color;
            promptText.color = color;
        }

        // Optional: tint background
        if (backgroundImage != null)
        {
            Color bgColor = color;
            bgColor.a = 0.3f; // Semi-transparent background
            backgroundImage.color = bgColor;
        }

        pulseTimer = 0f;
    }

    /// <summary>
    /// Show prompt with text only (white color default).
    /// </summary>
    public void Show(string text)
    {
        Show(text, Color.white);
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Update prompt text without hiding/showing (smooth updates).
    /// </summary>
    public void UpdateText(string text, Color color)
    {
        if (promptText != null)
        {
            promptText.text = text;
            currentColor = color;

            if (!enablePulse)
                promptText.color = color;
        }

        if (backgroundImage != null)
        {
            Color bgColor = color;
            bgColor.a = 0.3f;
            backgroundImage.color = bgColor;
        }
    }

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;
}