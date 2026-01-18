using System.Collections;
using UnityEngine;

/// <summary>
/// Visual feedback for security camera state.
/// Shows green/yellow/red indicator based on suspicion level.
/// </summary>
public class SecurityCameraIndicator : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private MeshRenderer indicatorMesh;
    [SerializeField] private Material idleMaterial;       // Green
    [SerializeField] private Material suspiciousMaterial; // Yellow
    [SerializeField] private Material alertMaterial;      // Red

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 1f;

    private Coroutine pulseCoroutine;
    private Material currentMaterial;

    private void Awake()
    {
        if (indicatorMesh == null)
            indicatorMesh = GetComponent<MeshRenderer>();
    }

    /// <summary>
    /// Set indicator state (changes material).
    /// </summary>
    public void SetState(CameraState state)
    {
        // Stop pulse if running
        StopPulse();

        switch (state)
        {
            case CameraState.Idle:
                SetMaterial(idleMaterial);
                break;

            case CameraState.Suspicious:
                SetMaterial(suspiciousMaterial);
                break;

            case CameraState.Alert:
                SetMaterial(alertMaterial);
                StartPulse();
                break;
        }
    }

    private void SetMaterial(Material material)
    {
        if (indicatorMesh != null && material != null)
        {
            indicatorMesh.material = material;
            currentMaterial = material;
        }
    }

    /// <summary>
    /// Start pulsing effect (for alert state).
    /// </summary>
    private void StartPulse()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(PulseCoroutine());
    }

    /// <summary>
    /// Stop pulsing effect.
    /// </summary>
    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        // Reset emission
        if (currentMaterial != null && currentMaterial.HasProperty("_EmissionColor"))
        {
            currentMaterial.SetColor("_EmissionColor", Color.black);
        }
    }

    private IEnumerator PulseCoroutine()
    {
        while (true)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f) * pulseIntensity;

            if (currentMaterial != null && currentMaterial.HasProperty("_EmissionColor"))
            {
                Color emissionColor = Color.red * pulse;
                currentMaterial.SetColor("_EmissionColor", emissionColor);
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        StopPulse();
    }
}

/// <summary>
/// Camera state enum (shared with SecurityCamera).
/// </summary>
public enum CameraState
{
    Idle,       // Green - patrolling
    Suspicious, // Yellow - player detected (0-100%)
    Alert       // Red - alarm triggered
}