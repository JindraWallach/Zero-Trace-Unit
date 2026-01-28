using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;

/// <summary>
/// Modifies movement speeds on top of SamplePlayerAnimationController.
/// Attach to Player prefab alongside SamplePlayerAnimationController.
/// SRP: Only manages speed multipliers, doesn't touch original controller.
/// </summary>
public class PlayerMovementModifier : MonoBehaviour
{
    [Header("Base Speeds (Set these to match SamplePlayerAnimationController)")]
    [Tooltip("Base walk speed from SamplePlayerAnimationController")]
    [SerializeField] private float baseWalkSpeed = 1.4f;

    [Tooltip("Base run speed from SamplePlayerAnimationController")]
    [SerializeField] private float baseRunSpeed = 2.5f;

    [Tooltip("Base sprint speed from SamplePlayerAnimationController")]
    [SerializeField] private float baseSprintSpeed = 7f;

    [Header("Current Multiplier")]
    [Tooltip("Current speed multiplier applied (read-only)")]
    [SerializeField] private float currentMultiplier = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private SamplePlayerAnimationController animController;
    private bool isModified = false;

    private void Awake()
    {
        animController = GetComponent<SamplePlayerAnimationController>();

        if (animController == null)
        {
            Debug.LogError("[PlayerMovementModifier] SamplePlayerAnimationController not found!");
            enabled = false;
        }
    }

    /// <summary>
    /// Apply speed multiplier.
    /// Called by PlayerClassApplier.
    /// </summary>
    public void ApplySpeedMultiplier(float multiplier)
    {
        if (isModified)
        {
            Debug.LogWarning("[PlayerMovementModifier] Speed already modified! Call ResetSpeeds() first.");
            return;
        }

        currentMultiplier = multiplier;

        // Calculate modified speeds
        float modifiedWalkSpeed = baseWalkSpeed * multiplier;
        float modifiedRunSpeed = baseRunSpeed * multiplier;
        float modifiedSprintSpeed = baseSprintSpeed * multiplier;

        // Apply via reflection (doesn't require public fields)
        SetPrivateField("_walkSpeed", modifiedWalkSpeed);
        SetPrivateField("_runSpeed", modifiedRunSpeed);
        SetPrivateField("_sprintSpeed", modifiedSprintSpeed);

        isModified = true;

        if (debugLog)
        {
            Debug.Log($"[PlayerMovementModifier] Applied {multiplier:F2}x multiplier:");
            Debug.Log($"  Walk: {baseWalkSpeed:F2} → {modifiedWalkSpeed:F2}");
            Debug.Log($"  Run: {baseRunSpeed:F2} → {modifiedRunSpeed:F2}");
            Debug.Log($"  Sprint: {baseSprintSpeed:F2} → {modifiedSprintSpeed:F2}");
        }
    }

    /// <summary>
    /// Reset speeds to base values.
    /// </summary>
    public void ResetSpeeds()
    {
        if (!isModified) return;

        SetPrivateField("_walkSpeed", baseWalkSpeed);
        SetPrivateField("_runSpeed", baseRunSpeed);
        SetPrivateField("_sprintSpeed", baseSprintSpeed);

        currentMultiplier = 1.0f;
        isModified = false;

        if (debugLog)
            Debug.Log("[PlayerMovementModifier] Reset to base speeds");
    }

    /// <summary>
    /// Set private field using reflection.
    /// Works without making fields public.
    /// </summary>
    private void SetPrivateField(string fieldName, float value)
    {
        var field = typeof(SamplePlayerAnimationController).GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (field != null)
        {
            field.SetValue(animController, value);
        }
        else
        {
            Debug.LogWarning($"[PlayerMovementModifier] Field '{fieldName}' not found in SamplePlayerAnimationController!");
        }
    }

    /// <summary>
    /// Get current multiplier.
    /// </summary>
    public float GetCurrentMultiplier()
    {
        return currentMultiplier;
    }

    /// <summary>
    /// Check if speeds are modified.
    /// </summary>
    public bool IsModified()
    {
        return isModified;
    }
}