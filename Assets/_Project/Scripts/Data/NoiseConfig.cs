using UnityEngine;

/// <summary>
/// ScriptableObject configuration for noise detection system.
/// Defines noise radii for different player actions.
/// Create via: Assets > Create > Zero Trace > Noise Config
/// </summary>
[CreateAssetMenu(fileName = "NoiseConfig", menuName = "Zero Trace/Noise Config")]
public class NoiseConfig : ScriptableObject
{
    [Header("Footsteps")]
    [Tooltip("Noise radius when walking (meters)")]
    [Range(0f, 10f)]
    public float walkNoiseRadius = 3f;

    [Tooltip("Noise radius when running (meters)")]
    [Range(5f, 20f)]
    public float runNoiseRadius = 10f;

    [Tooltip("Interval between footstep noise when walking (seconds)")]
    [Range(0.3f, 1f)]
    public float walkFootstepInterval = 0.5f;

    [Tooltip("Interval between footstep noise when running (seconds)")]
    [Range(0.1f, 0.5f)]
    public float runFootstepInterval = 0.3f;

    [Header("Landing")]
    [Tooltip("Minimum fall velocity to make noise (units/s)")]
    [Range(0f, 10f)]
    public float minFallVelocityForNoise = 5f;

    [Tooltip("Noise radius for soft landing")]
    [Range(3f, 10f)]
    public float minLandingRadius = 5f;

    [Tooltip("Noise radius for hard landing")]
    [Range(10f, 30f)]
    public float maxLandingRadius = 15f;

    [Tooltip("Fall velocity for maximum noise (units/s)")]
    [Range(10f, 30f)]
    public float maxFallVelocity = 20f;

    [Header("Doors")]
    [Tooltip("Noise radius when opening door")]
    [Range(3f, 15f)]
    public float doorOpenRadius = 8f;

    [Tooltip("Noise radius when closing door")]
    [Range(3f, 15f)]
    public float doorCloseRadius = 6f;

    [Header("Debug")]
    [Tooltip("Show noise gizmos in Scene view")]
    public bool debugNoise = true;

    [Tooltip("Duration to show noise gizmos (seconds)")]
    [Range(0.5f, 5f)]
    public float debugNoiseDuration = 2f;

    private void OnValidate()
    {
        walkNoiseRadius = Mathf.Max(0.1f, walkNoiseRadius);
        runNoiseRadius = Mathf.Max(walkNoiseRadius, runNoiseRadius);
        minLandingRadius = Mathf.Max(1f, minLandingRadius);
        maxLandingRadius = Mathf.Max(minLandingRadius, maxLandingRadius);
    }
}