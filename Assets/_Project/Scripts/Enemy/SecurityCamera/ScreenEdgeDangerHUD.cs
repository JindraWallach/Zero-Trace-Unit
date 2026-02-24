using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a full-screen sliced border Image red based on highest camera suspicion.
/// borderImage MUST be on a separate Canvas (or outside any CanvasGroup)
/// so SecurityCameraHUD's CanvasGroup.alpha cannot override it.
///
/// Single Responsibility: border visual only. No warning text, no CanvasGroup.
/// </summary>
public class ScreenEdgeDangerHUD : MonoBehaviour
{
    public static ScreenEdgeDangerHUD Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private ScreenEdgeDangerConfig config;

    [Header("References")]
    [SerializeField] private Image borderImage;

    private float currentSuspicion;
    private Coroutine updateCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (borderImage == null)
        {
            Debug.LogError("[ScreenEdgeDangerHUD] Missing borderImage!", this);
            enabled = false;
            return;
        }
        if (config == null)
        {
            Debug.LogError("[ScreenEdgeDangerHUD] Missing ScreenEdgeDangerConfig!", this);
            enabled = false;
            return;
        }

        ApplyImmediate(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void RegisterCamera(SecurityCamera camera)
    {
        if (camera == null) return;
        camera.OnSuspicionChanged += OnSuspicionChanged;
    }

    public void UnregisterCamera(SecurityCamera camera)
    {
        if (camera == null) return;
        camera.OnSuspicionChanged -= OnSuspicionChanged;
    }

    public void ResetHUD()
    {
        currentSuspicion = 0f;
        StopUpdateCoroutine();
        ApplyImmediate(0f);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnSuspicionChanged(float suspicion)
    {
        // Keep highest suspicion if multiple cameras
        if (suspicion > currentSuspicion || suspicion == 0f)
            currentSuspicion = suspicion;

        // Start coroutine if not running
        if (updateCoroutine == null)
            updateCoroutine = StartCoroutine(BorderUpdateCoroutine());
    }

    private IEnumerator BorderUpdateCoroutine()
    {
        float currentAlpha = borderImage.color.a;

        while (true)
        {
            float targetAlpha = config.GetTargetAlpha(currentSuspicion);
            float targetPixels = config.GetBorderPixels(currentSuspicion);

            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, config.lerpSpeed * Time.deltaTime);

            Color c = config.borderColor;
            c.a = currentAlpha;
            borderImage.color = c;
            borderImage.pixelsPerUnitMultiplier = Mathf.Max(0.01f, targetPixels);

            // Stop when fully faded out and suspicion is 0
            if (currentSuspicion <= 0f && currentAlpha < 0.001f)
            {
                ApplyImmediate(0f);
                updateCoroutine = null;
                yield break;
            }

            yield return null;
        }
    }

    private void ApplyImmediate(float alpha)
    {
        Color c = config != null ? config.borderColor : Color.red;
        c.a = alpha;
        borderImage.color = c;
        if (config != null)
            borderImage.pixelsPerUnitMultiplier = config.minPixelsPerUnit;
    }

    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }
}