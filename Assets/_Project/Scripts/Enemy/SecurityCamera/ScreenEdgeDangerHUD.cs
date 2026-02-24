using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a full-screen sliced border Image red based on highest camera suspicion.
/// borderImage MUST be outside any CanvasGroup in the hierarchy.
/// Single Responsibility: border visual only.
/// </summary>
public class ScreenEdgeDangerHUD : MonoBehaviour
{
    public static ScreenEdgeDangerHUD Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private ScreenEdgeDangerConfig config;

    [Header("References")]
    [SerializeField] private Image borderImage;

    // Highest suspicion reported this tick — resets every coroutine frame
    private float reportedSuspicion;
    private bool hasStarted;

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

        SetAlpha(0f);
    }

    private void Start()
    {
        // Always-running coroutine — decays naturally when no suspicion reported
        StartCoroutine(BorderCoroutine());
        hasStarted = true;
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
        reportedSuspicion = 0f;
        SetAlpha(0f);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnSuspicionChanged(float suspicion)
    {
        // Keep highest value reported this frame
        if (suspicion > reportedSuspicion)
            reportedSuspicion = suspicion;
    }

    private IEnumerator BorderCoroutine()
    {
        float currentAlpha = 0f;

        while (true)
        {
            float targetAlpha = config.GetTargetAlpha(reportedSuspicion);
            float targetPixels = config.GetBorderPixels(reportedSuspicion);

            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, config.lerpSpeed * Time.deltaTime);

            SetAlpha(currentAlpha);
            borderImage.pixelsPerUnitMultiplier = Mathf.Max(0.01f, targetPixels);

            // Reset reported suspicion each frame — cameras must re-report next frame
            // If no camera reports, suspicion = 0 and alpha lerps back to 0
            reportedSuspicion = 0f;

            yield return null;
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = config.borderColor;
        c.a = alpha;
        borderImage.color = c;
    }
}