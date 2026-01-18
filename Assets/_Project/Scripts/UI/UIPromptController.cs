using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space interaction prompt with dynamic text and color.
/// Supports formatted text with input keys and color coding.
/// Uses coroutine for pulse animation (performance-friendly).
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
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Hide();
    }

    private void OnDisable()
    {
        StopPulse();
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

        // Start pulse animation if enabled
        if (enablePulse)
            StartPulse();
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
        StopPulse();

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

            // Reset color immediately if pulse is disabled
            if (!enablePulse)
                promptText.color = color;
        }

        if (backgroundImage != null)
        {
            Color bgColor = color;
            bgColor.a = 0.3f;
            backgroundImage.color = bgColor;
        }

        // Restart pulse with new color
        if (enablePulse && canvasGroup != null && canvasGroup.alpha > 0)
        {
            StopPulse();
            StartPulse();
        }
    }

    private void StartPulse()
    {
        StopPulse();
        pulseCoroutine = StartCoroutine(PulseCoroutine());
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Reset to original color when stopping
        if (promptText != null)
            promptText.color = currentColor;
    }

    private IEnumerator PulseCoroutine()
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.deltaTime * pulseSpeed;
            float scale = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(timer) + 1f) * 0.5f);

            if (promptText != null)
            {
                Color pulsedColor = currentColor * scale;
                pulsedColor.a = currentColor.a; // Preserve alpha
                promptText.color = pulsedColor;
            }

            yield return null;
        }
    }

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;
}