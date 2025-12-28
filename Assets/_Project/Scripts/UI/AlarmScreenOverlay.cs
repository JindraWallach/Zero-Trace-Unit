using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI overlay for alarm - red pulsing vignette effect.
/// Separate component for UI concerns (SRP).
/// </summary>
[RequireComponent(typeof(Image))]
public class AlarmScreenOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SecurityAlarmSystem alarmSystem;
    [SerializeField] private SecurityAlarmConfig config;

    private Image overlayImage;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        overlayImage = GetComponent<Image>();
        overlayImage.color = new Color(1f, 0f, 0f, 0f); // Red, transparent

        // Auto-find alarm system if not assigned
        if (alarmSystem == null)
            alarmSystem = FindFirstObjectByType<SecurityAlarmSystem>();

        if (alarmSystem == null)
        {
            Debug.LogError("[AlarmScreenOverlay] No SecurityAlarmSystem found!", this);
            enabled = false;
            return;
        }

        // Subscribe to events
        alarmSystem.OnAlarmTriggered += StartOverlay;
        alarmSystem.OnAlarmEnded += StopOverlay;
    }

    private void OnDestroy()
    {
        if (alarmSystem != null)
        {
            alarmSystem.OnAlarmTriggered -= StartOverlay;
            alarmSystem.OnAlarmEnded -= StopOverlay;
        }
    }

    private void StartOverlay()
    {
        if (!config.enableScreenOverlay) return;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(PulseOverlayCoroutine());
    }

    private void StopOverlay()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Fade out
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator PulseOverlayCoroutine()
    {
        float time = 0f;

        while (true)
        {
            // Sine wave pulse
            float alpha = (Mathf.Sin(time * config.overlayPulseSpeed) + 1f) * 0.5f;
            alpha *= config.overlayMaxAlpha;

            overlayImage.color = new Color(1f, 0f, 0f, alpha);

            time += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeOutCoroutine()
    {
        Color startColor = overlayImage.color;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            overlayImage.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }

        overlayImage.color = new Color(1f, 0f, 0f, 0f);
    }
}
