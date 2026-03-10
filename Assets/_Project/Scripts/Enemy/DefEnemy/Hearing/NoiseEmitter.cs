using UnityEngine;
using ZeroTrace.Audio;

/// <summary>
/// Component for emitting noise from player actions.
/// Attach to player GameObject.
/// Integrates with player movement and door interactions.
/// Audio přes AudioManager — žádný vlastní AudioSource/AudioClip.
/// </summary>
public class NoiseEmitter : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private NoiseConfig config;

    [Header("Audio IDs")]
    [Tooltip("Zvuk kroků při chůzi")]
    [SerializeField] private string walkStepSoundId = "footstep_walk";
    [Tooltip("Zvuk kroků při běhu")]
    [SerializeField] private string runStepSoundId = "footstep_run";
    [Tooltip("Zvuk přistání (lehké / pomalý pád)")]
    [SerializeField] private string landSoftSoundId = "land_soft";
    [Tooltip("Zvuk přistání (tvrdé / rychlý pád)")]
    [SerializeField] private string landHardSoundId = "land_hard";
    [Tooltip("Zvuk přepnutí baterky")]
    [SerializeField] private string flashlightSoundId = "flashlight_toggle";

    [Header("Debug")]
    [SerializeField] private float timeSinceLastFootstep;
    [SerializeField] private bool isMoving;
    [SerializeField] private bool isRunning;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError($"[NoiseEmitter] {name} missing NoiseConfig!", this);
            enabled = false;
        }
    }

    // ── Public API ───────────────────────────────────────────────────────────

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

        // Emit noise + sound if interval passed
        if (timeSinceLastFootstep >= interval)
        {
            EmitFootstep(running);
            timeSinceLastFootstep = 0f;
        }
    }

    /// <summary>Emit footstep noise + přehraj zvuk kroků.</summary>
    public void EmitFootstep(bool running)
    {
        float radius = running ? config.runNoiseRadius : config.walkNoiseRadius;
        NoiseType type = running ? NoiseType.Running : NoiseType.Footsteps;

        NoiseSystem.Instance?.EmitNoise(transform.position, radius, type);

        // Zvuk přes AudioManager
        string soundId = running ? runStepSoundId : walkStepSoundId;
        AudioManager.Instance?.Play(soundId, transform.position);
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

        // Měkké vs tvrdé přistání — práh na 50 % rozsahu velocity
        string soundId = t < 0.5f ? landSoftSoundId : landHardSoundId;
        AudioManager.Instance?.Play(soundId, transform.position);
    }

    /// <summary>
    /// Emit door open noise.
    /// Call from door interaction.
    /// Pozn.: samotný dveřní zvuk přehrává DoorController — zde jen AI noise event.
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

    /// <summary>Emit flashlight toggle noise + přehraj zvuk baterky.</summary>
    public void EmitFlashlightSound(Vector3 playerPos)
    {
        NoiseSystem.Instance?.EmitNoise(playerPos, config.flashlightToggleRadius, NoiseType.FlashlightToggle);
        AudioManager.Instance?.Play(flashlightSoundId, playerPos);
    }
}