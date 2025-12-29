using UnityEngine;

/// <summary>
/// ScriptableObject configuration for security camera rotation.
/// Single source of truth for all rotation/tracking settings.
/// Create via: Assets > Create > Zero Trace > Security Camera Rotation Config
/// </summary>
[CreateAssetMenu(fileName = "SecurityCameraRotationConfig", menuName = "Zero Trace/Security Camera Rotation Config")]
public class SecurityCameraRotationConfig : ScriptableObject
{
    [Header("Sweep Settings")]
    [Tooltip("Left sweep angle (negative)")]
    [Range(-90f, 0f)]
    public float sweepAngleLeft = -60f;

    [Tooltip("Right sweep angle (positive)")]
    [Range(0f, 90f)]
    public float sweepAngleRight = 60f;

    [Tooltip("Sweep speed (degrees per second)")]
    [Range(10f, 60f)]
    public float sweepSpeed = 20f;

    [Tooltip("Pause at each end (seconds)")]
    [Range(0f, 3f)]
    public float pauseAtEnd = 0.5f;

    [Header("Tracking Settings")]
    [Tooltip("Rotation speed when tracking player (degrees/sec)")]
    [Range(30f, 120f)]
    public float trackingSpeed = 60f;

    [Header("Laser Pointer")]
    [Tooltip("Layer mask for laser raycasts")]
    public LayerMask laserHitMask = -1;

    [Tooltip("Laser max distance")]
    [Range(5f, 30f)]
    public float laserMaxDistance = 20f;

    [Tooltip("Laser point offset from surface (prevents z-fighting)")]
    [Range(0.001f, 0.1f)]
    public float laserSurfaceOffset = 0.01f;

    [Header("Debug")]
    [Tooltip("Show sweep range gizmos in Scene view")]
    public bool debugGizmos = true;

    private void OnValidate()
    {
        // Ensure logical values
        sweepAngleLeft = Mathf.Clamp(sweepAngleLeft, -90f, 0f);
        sweepAngleRight = Mathf.Clamp(sweepAngleRight, 0f, 90f);
        sweepSpeed = Mathf.Max(1f, sweepSpeed);
        trackingSpeed = Mathf.Max(1f, trackingSpeed);
        laserMaxDistance = Mathf.Max(1f, laserMaxDistance);
    }
}