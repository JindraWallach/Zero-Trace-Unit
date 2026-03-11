using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD warning system for security camera detection.
/// Shows warning when player is detected (suspicion >= 100%).
/// Singleton pattern for easy access from cameras.
/// </summary>
public class SecurityCameraHUD : MonoBehaviour
{
    public static SecurityCameraHUD Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup warningCanvasGroup;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Image warningBackground;

    [Header("Suspicion Bar (Optional)")]
    [SerializeField] private bool showSuspicionBar = false;
    [SerializeField] private Slider suspicionSlider;
    [SerializeField] private Image suspicionFillImage;
    [SerializeField] private Gradient suspicionGradient;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Warning Text")]
    [SerializeField] private string warningMessage = "⚠ DETECTED BY CAMERA ⚠";
    [SerializeField] private Color warningColor = Color.red;

    [Header("Pulse Alpha Ranges")]
    [SerializeField][Range(0f, 1f)] private float textPulseMin = 0.5f;
    [SerializeField][Range(0f, 1f)] private float textPulseMax = 1f;
    [SerializeField][Range(0f, 1f)] private float backgroundPulseMin = 0.3f;
    [SerializeField][Range(0f, 1f)] private float backgroundPulseMax = 0.7f;

    private Coroutine fadeCoroutine;
    private Coroutine pulseCoroutine;
    private bool isWarningVisible;
    private float _lastSliderValue = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (warningCanvasGroup == null)
        {
            Debug.LogError("[SecurityCameraHUD] Missing CanvasGroup reference!", this);
            enabled = false;
            return;
        }

        warningCanvasGroup.alpha = 0f;
        warningCanvasGroup.interactable = false;
        warningCanvasGroup.blocksRaycasts = false;
        isWarningVisible = false;

        if (showSuspicionBar && suspicionSlider != null)
            suspicionSlider.gameObject.SetActive(true);
        else if (suspicionSlider != null)
            suspicionSlider.gameObject.SetActive(false);

        if (warningText != null)
        {
            warningText.text = warningMessage;
            warningText.color = warningColor;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // === PUBLIC API ===

    public void ShowWarning()
    {
        if (isWarningVisible) return;
        isWarningVisible = true;
        StopAllAnimations();
        fadeCoroutine = StartCoroutine(FadeIn());
        pulseCoroutine = StartCoroutine(PulseWarning());
    }

    public void HideWarning()
    {
        if (!isWarningVisible) return;
        isWarningVisible = false;
        StopAllAnimations();
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    public void UpdateSuspicionBar(float suspicionPercent)
    {
        if (!showSuspicionBar || suspicionSlider == null) return;

        suspicionPercent = Mathf.Clamp(suspicionPercent, 0f, 100f);
        float normalized = suspicionPercent / 100f;

        // Neaktualizuj UI pokud se hodnota nezměnila (dirty check)
        if (Mathf.Approximately(_lastSliderValue, normalized)) return;
        _lastSliderValue = normalized;

        suspicionSlider.value = normalized;

        if (suspicionFillImage != null && suspicionGradient != null)
            suspicionFillImage.color = suspicionGradient.Evaluate(normalized);

        if (suspicionPercent <= 0f && isWarningVisible)
            HideWarning();
    }

    public void ResetHUD()
    {
        HideWarning();
        UpdateSuspicionBar(0f);
    }

    // === ANIMATION COROUTINES ===

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            warningCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            warningCanvasGroup.interactable = true;
            warningCanvasGroup.blocksRaycasts = true;
            yield return null;
        }
        warningCanvasGroup.alpha = 1f;
        fadeCoroutine = null;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = warningCanvasGroup.alpha;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            warningCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        warningCanvasGroup.alpha = 0f;
        warningCanvasGroup.interactable = false;
        warningCanvasGroup.blocksRaycasts = false;
        fadeCoroutine = null;
    }

    private IEnumerator PulseWarning()
    {
        if (warningText == null && warningBackground == null) yield break;

        Color originalTextColor = warningText != null ? warningText.color : Color.white;
        Color originalBgColor = warningBackground != null ? warningBackground.color : Color.white;

        while (isWarningVisible)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);

            if (warningText != null)
            {
                Color c = originalTextColor;
                c.a = Mathf.Lerp(textPulseMin, textPulseMax, pulse);
                warningText.color = c;
            }

            if (warningBackground != null)
            {
                Color c = originalBgColor;
                c.a = Mathf.Lerp(backgroundPulseMin, backgroundPulseMax, pulse);
                warningBackground.color = c;
            }

            yield return null;
        }

        if (warningText != null) warningText.color = originalTextColor;
        if (warningBackground != null) warningBackground.color = originalBgColor;

        pulseCoroutine = null;
    }

    private void StopAllAnimations()
    {
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
    }

    // === INTEGRATION ===

    public void RegisterCamera(SecurityCamera camera)
    {
        if (camera == null) return;
        camera.OnAlertTriggered += ShowWarning;
        if (showSuspicionBar)
            camera.OnSuspicionChanged += UpdateSuspicionBar;
    }

    public void UnregisterCamera(SecurityCamera camera)
    {
        if (camera == null) return;
        camera.OnAlertTriggered -= ShowWarning;
        if (showSuspicionBar)
            camera.OnSuspicionChanged -= UpdateSuspicionBar;
    }
}