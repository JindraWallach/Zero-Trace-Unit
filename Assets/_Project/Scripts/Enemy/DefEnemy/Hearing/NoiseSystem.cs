using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central noise event manager (singleton).
/// Broadcasts noise events to all listeners (enemies).
/// Handles visual debug gizmos for noise propagation.
/// </summary>
public class NoiseSystem : MonoBehaviour
{
    public static NoiseSystem Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private NoiseConfig config;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private List<NoiseDebugInfo> activeNoises = new List<NoiseDebugInfo>();

    // Events
    public event Action<Vector3, float, NoiseType> OnNoiseMade;

    // Debug visualization data
    private class NoiseDebugInfo
    {
        public Vector3 position;
        public float radius;
        public NoiseType type;
        public float timestamp;
        public Color color;

        public NoiseDebugInfo(Vector3 pos, float rad, NoiseType t, float time, Color col)
        {
            position = pos;
            radius = rad;
            type = t;
            timestamp = time;
            color = col;
        }
    }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Validate config
        if (config == null)
        {
            Debug.LogError("[NoiseSystem] Missing NoiseConfig!", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Emit noise at position with given radius.
    /// Notifies all registered listeners (enemies).
    /// </summary>
    public void EmitNoise(Vector3 position, float radius, NoiseType type)
    {
        // Fire event to listeners
        OnNoiseMade?.Invoke(position, radius, type);

        // Debug logging
        if (showDebugLogs)
        {
            Debug.Log($"[NoiseSystem] Noise emitted: {type} at {position} (radius: {radius:F1}m)");
        }

        // Add to debug visualization
        if (config.debugNoise)
        {
            Color debugColor = GetNoiseColor(type);
            activeNoises.Add(new NoiseDebugInfo(position, radius, type, Time.time, debugColor));

            // Cleanup old noises
            activeNoises.RemoveAll(n => Time.time - n.timestamp > config.debugNoiseDuration);
        }
    }

    /// <summary>
    /// Get noise color for debug visualization.
    /// </summary>
    /// <summary>
    /// Get noise color for debug visualization.
    /// </summary>
    private Color GetNoiseColor(NoiseType type)
    {
        switch (type)
        {
            case NoiseType.Footsteps:
                return Color.yellow;
            case NoiseType.Running:
                return Color.red;
            case NoiseType.Landing:
                return Color.magenta;
            case NoiseType.DoorOpen:
                return Color.cyan;
            case NoiseType.DoorClose:
                return Color.blue;
            case NoiseType.FlashlightToggle:
                return Color.green;
            default:
                return Color.white;
        }
    }

    // === DEBUG GIZMOS ===

    private void OnDrawGizmos()
    {
        if (config == null || !config.debugNoise)
            return;

        // Draw all active noises
        foreach (var noise in activeNoises)
        {
            float age = Time.time - noise.timestamp;
            float alpha = 1f - (age / config.debugNoiseDuration);
            Color color = noise.color;
            color.a = alpha * 0.5f;

            Gizmos.color = color;

            // Draw sphere
            Gizmos.DrawWireSphere(noise.position, noise.radius);

            // Draw expanding ripple effect
            float rippleRadius = Mathf.Lerp(0f, noise.radius, age / config.debugNoiseDuration);
            Gizmos.DrawWireSphere(noise.position, rippleRadius);

            // Draw vertical line
            Gizmos.DrawLine(noise.position, noise.position + Vector3.up * 3f);

#if UNITY_EDITOR
            // Draw label
            UnityEditor.Handles.Label(
                noise.position + Vector3.up * 3.5f,
                $"{noise.type}\n{noise.radius:F1}m",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = noise.color },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                }
            );
#endif
        }
    }
}

/// <summary>
/// Types of noise that can be emitted.
/// </summary>
/// <summary>
/// Types of noise that can be emitted.
/// </summary>
public enum NoiseType
{
    Footsteps,      // Walking footsteps
    Running,        // Running footsteps
    Landing,        // Landing from jump/fall
    DoorOpen,       // Opening door
    DoorClose,      // Closing door
    FlashlightToggle   // Flashlight 
}