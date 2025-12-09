using UnityEngine;

/// <summary>
/// Component for emitting noise from player actions.
/// Attach to player GameObject.
/// Integrates with player movement and door interactions.
/// </summary>
public class NoiseEmitter : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private NoiseConfig config;

    [Header("Debug")]
    [SerializeField] private float timeSinceLastFootstep;
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isRunning;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"[NoiseEmitter] {name} missing NoiseConfig!", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Call from player movement Update().
    /// Emits footstep noise based on movement state.
    /// </summary>
    public void UpdateFootsteps(bool moving, bool running, bool grounded)
    {
        isMoving = moving;
        isRunning = running;

        if (!grounded || !moving)
        {
            timeSinceLastFootstep = 0f;
            return;
        }

        // Accumulate time
        timeSinceLastFootstep += Time.deltaTime;

        // Determine interval
        float interval = running ? config.runFootstepInterval : config.walkFootstepInterval;

        // Emit noise if interval passed
        if (timeSinceLastFootstep >= interval)
        {
            EmitFootstep(running);
            timeSinceLastFootstep = 0f;
        }
    }

    /// <summary>
    /// Emit footstep noise.
    /// </summary>
    public void EmitFootstep(bool running)
    {
        float radius = running ? config.runNoiseRadius : config.walkNoiseRadius;
        NoiseType type = running ? NoiseType.Running : NoiseType.Footsteps;

        NoiseSystem.Instance?.EmitNoise(transform.position, radius, type);
    }

    /// <summary>
    /// Emit landing noise based on fall velocity.
    /// Call from player OnLanded() callback.
    /// </summary>
    public void EmitLanding(float fallVelocity)
    {
        // Only make noise if fall is significant
        if (fallVelocity < config.minFallVelocityForNoise)
            return;

        // Calculate radius based on fall velocity
        float t = Mathf.InverseLerp(config.minFallVelocityForNoise, config.maxFallVelocity, fallVelocity);
        float radius = Mathf.Lerp(config.minLandingRadius, config.maxLandingRadius, t);

        NoiseSystem.Instance?.EmitNoise(transform.position, radius, NoiseType.Landing);
    }

    /// <summary>
    /// Emit door open noise.
    /// Call from door interaction.
    /// </summary>
    public void EmitDoorOpen(Vector3 doorPosition)
    {
        NoiseSystem.Instance?.EmitNoise(doorPosition, config.doorOpenRadius, NoiseType.DoorOpen);
    }

    /// <summary>
    /// Emit door close noise.
    /// Call from door interaction.
    /// </summary>
    public void EmitDoorClose(Vector3 doorPosition)
    {
        NoiseSystem.Instance?.EmitNoise(doorPosition, config.doorCloseRadius, NoiseType.DoorClose);
    }
}