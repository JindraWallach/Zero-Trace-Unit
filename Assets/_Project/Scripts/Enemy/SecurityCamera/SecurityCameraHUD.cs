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

    private Coroutine fadeCoroutine;
    private Coroutine pulseCoroutine;
    private bool isWarningVisible;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Validate references
        if (warningCanvasGroup == null)
        {
            Debug.LogError("[SecurityCameraHUD] Missing CanvasGroup reference!", this);
            enabled = false;
            return;
        }

        // Initialize hidden
        warningCanvasGroup.alpha = 0f;
        warningCanvasGroup.interactable = false;
        warningCanvasGroup.blocksRaycasts = false;
        isWarningVisible = false;

        // Setup suspicion bar
        if (showSuspicionBar && suspicionSlider != null)
        {
            suspicionSlider.gameObject.SetActive(true);
            suspicionSlider.value = 0f;
        }
        else if (suspicionSlider != null)
        {
            suspicionSlider.gameObject.SetActive(false);
        }

        // Setup warning text
        if (warningText != null)
        {
            warningText.text = warningMessage;
            warningText.color = warningColor;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Show warning overlay (called when camera triggers alert).
    /// </summary>
    public void ShowWarning()
    {
        if (isWarningVisible)
            return;

        isWarningVisible = true;

        // Stop any running animations
        StopAllAnimations();

        // Fade in
        fadeCoroutine = StartCoroutine(FadeIn());

        // Start pulse effect
        pulseCoroutine = StartCoroutine(PulseWarning());

        Debug.Log("[SecurityCameraHUD] Warning shown");
    }

    /// <summary>
    /// Hide warning overlay.
    /// </summary>
    public void HideWarning()
    {
        if (!isWarningVisible)
            return;

        isWarningVisible = false;

        // Stop pulse
        StopAllAnimations();

        // Fade out
        fadeCoroutine = StartCoroutine(FadeOut());

        Debug.Log("[SecurityCameraHUD] Warning hidden");
    }

    /// <summary>
    /// Update suspicion bar value (0-100).
    /// Optional feature for showing build-up before alert.
    /// </summary>
    public void UpdateSuspicionBar(float suspicionPercent)
    {
        if (!showSuspicionBar || suspicionSlider == null)
            return;

        suspicionPercent = Mathf.Clamp(suspicionPercent, 0f, 100f);
        suspicionSlider.value = suspicionPercent / 100f;

        // Update color gradient
        if (suspicionFillImage != null && suspicionGradient != null)
        {
            suspicionFillImage.color = suspicionGradient.Evaluate(suspicionPercent / 100f);
            //Debug.Log($"[SecurityCameraHUD] Suspicion bar updated: {suspicionPercent}%");
        }

        // Auto-hide warning if suspicion drops to 0 (player escaped before alert)
        // TODO: When AlarmSystem added, this behavior changes - alarm persists even if player escapes
        if (suspicionPercent <= 0f && isWarningVisible)
        {
            HideWarning();
        }
    }

    /// <summary>
    /// Reset HUD to initial state.
    /// </summary>
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
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);

            warningCanvasGroup.alpha = alpha;
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
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);

            warningCanvasGroup.alpha = alpha;

            yield return null;
        }

        warningCanvasGroup.alpha = 0f;
        warningCanvasGroup.interactable = false;
        warningCanvasGroup.blocksRaycasts = false;

        fadeCoroutine = null;
    }

    private IEnumerator PulseWarning()
    {
        if (warningText == null && warningBackground == null)
            yield break;

        Color originalTextColor = warningText != null ? warningText.color : Color.white;
        Color originalBgColor = warningBackground != null ? warningBackground.color : Color.white;

        while (isWarningVisible)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);

            // Pulse text alpha
            if (warningText != null)
            {
                Color textColor = originalTextColor;
                textColor.a = Mathf.Lerp(0.5f, 1f, pulse);
                warningText.color = textColor;
            }

            // Pulse background alpha
            if (warningBackground != null)
            {
                Color bgColor = originalBgColor;
                bgColor.a = Mathf.Lerp(0.3f, 0.7f, pulse);
                warningBackground.color = bgColor;
            }

            yield return null;
        }

        // Reset colors
        if (warningText != null)
            warningText.color = originalTextColor;

        if (warningBackground != null)
            warningBackground.color = originalBgColor;

        pulseCoroutine = null;
    }

    private void StopAllAnimations()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    // === INTEGRATION HELPER ===

    /// <summary>
    /// Subscribe to camera events automatically.
    /// Call this from SecurityCamera.Start() for easy integration.
    /// </summary>
    public void RegisterCamera(SecurityCamera camera)
    {
        if (camera == null)
            return;

        // Subscribe to alert event
        camera.OnAlertTriggered += ShowWarning;

        // Subscribe to suspicion changes (optional)
        if (showSuspicionBar)
        {
            camera.OnSuspicionChanged += UpdateSuspicionBar;
        }

        Debug.Log($"[SecurityCameraHUD] Registered camera: {camera.name}");
    }

    /// <summary>
    /// Unsubscribe from camera events.
    /// </summary>
    public void UnregisterCamera(SecurityCamera camera)
    {
        if (camera == null)
            return;

        camera.OnAlertTriggered -= ShowWarning;

        if (showSuspicionBar)
        {
            camera.OnSuspicionChanged -= UpdateSuspicionBar;
        }

        Debug.Log($"[SecurityCameraHUD] Unregistered camera: {camera.name}");
    }
}