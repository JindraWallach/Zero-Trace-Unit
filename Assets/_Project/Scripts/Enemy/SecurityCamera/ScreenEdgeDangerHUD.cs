using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a full-screen sliced border Image that turns red based on the highest
/// active camera suspicion level.
/// 
/// Setup:
///   • Add this component to a Canvas child GameObject.
///   • Assign a 9-sliced border sprite to borderImage (stretch mode = Sliced).
///   • Assign ScreenEdgeDangerConfig SO.
///   • ScreenEdgeDangerHUD.Instance is populated automatically (per-scene singleton).
/// 
/// Single Responsibility: update one UI Image alpha + border size from suspicion data.
/// Performance: no Update() allocation; runs only when suspicion changes via event.
/// </summary>
public class ScreenEdgeDangerHUD : MonoBehaviour
{
    public static ScreenEdgeDangerHUD Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private ScreenEdgeDangerConfig config;

    [Header("References")]
    [SerializeField] private Image borderImage;

    // Current smoothed alpha
    private float currentAlpha;

    // Highest suspicion seen this frame across all registered cameras
    private float highestSuspicion;

    // Whether we need to re-render this frame
    private bool dirty;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (borderImage == null)
        {
            Debug.LogError("[ScreenEdgeDangerHUD] Missing borderImage reference!", this);
            enabled = false;
            return;
        }

        if (config == null)
        {
            Debug.LogError("[ScreenEdgeDangerHUD] Missing ScreenEdgeDangerConfig!", this);
            enabled = false;
            return;
        }

        // Initialise hidden
        SetBorderImmediate(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;

        ApplyBorder(highestSuspicion);

        // Reset for next frame
        highestSuspicion = 0f;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by SecurityCamera (or SecurityCameraHUD) every time suspicion changes.
    /// Keeps the highest value across all cameras per frame.
    /// </summary>
    public void ReportSuspicion(float suspicion)
    {
        if (suspicion > highestSuspicion)
        {
            highestSuspicion = suspicion;
            dirty = true;
        }
    }

    /// <summary>
    /// Register a SecurityCamera so its suspicion is forwarded here automatically.
    /// </summary>
    public void RegisterCamera(SecurityCamera camera)
    {
        if (camera == null) return;
        camera.OnSuspicionChanged += ReportSuspicion;
    }

    /// <summary>
    /// Unregister a SecurityCamera.
    /// </summary>
    public void UnregisterCamera(SecurityCamera camera)
    {
        if (camera == null) return;
        camera.OnSuspicionChanged -= ReportSuspicion;
    }

    /// <summary>
    /// Instantly hide the border (e.g., on scene/game reset).
    /// </summary>
    public void ResetHUD()
    {
        highestSuspicion = 0f;
        dirty = false;
        SetBorderImmediate(0f);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void ApplyBorder(float suspicion)
    {
        float targetAlpha = config.GetTargetAlpha(suspicion);
        float targetPixels = config.GetBorderPixels(suspicion);

        // Smooth alpha toward target
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime / Mathf.Max(config.lerpSpeed, 0.001f));

        // Apply color + alpha
        Color c = config.borderColor;
        c.a = currentAlpha;
        borderImage.color = c;

        // Apply border size (9-sliced pixel inset)
        borderImage.pixelsPerUnitMultiplier = Mathf.Max(0.01f, targetPixels);
    }

    private void SetBorderImmediate(float suspicion)
    {
        currentAlpha = 0f;

        Color c = config != null ? config.borderColor : Color.red;
        c.a = 0f;
        borderImage.color = c;

        if (config != null)
            borderImage.pixelsPerUnitMultiplier = config.minBorderPixels;
    }
}