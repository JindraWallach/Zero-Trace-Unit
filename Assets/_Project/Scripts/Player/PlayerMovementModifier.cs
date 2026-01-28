using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;

public class PlayerMovementModifier : MonoBehaviour
{
    [Header("Base Speeds (Match SamplePlayerAnimationController)")]
    [SerializeField] private float baseWalkSpeed = 1.4f;
    [SerializeField] private float baseRunSpeed = 2.5f;
    [SerializeField] private float baseSprintSpeed = 7f;

    [Header("Current Multiplier")]
    [SerializeField] private float currentMultiplier = 1.0f;
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

    public void ApplySpeedMultiplier(float multiplier)
    {
        if (isModified)
        {
            ResetSpeeds();
        }

        currentMultiplier = multiplier;
        SetPrivateField("_walkSpeed", baseWalkSpeed * multiplier);
        SetPrivateField("_runSpeed", baseRunSpeed * multiplier);
        SetPrivateField("_sprintSpeed", baseSprintSpeed * multiplier);
        isModified = true;

        if (debugLog)
            Debug.Log($"[PlayerMovementModifier] Applied {multiplier:F2}x speed multiplier");
    }

    public void ResetSpeeds()
    {
        if (!isModified) return;
        SetPrivateField("_walkSpeed", baseWalkSpeed);
        SetPrivateField("_runSpeed", baseRunSpeed);
        SetPrivateField("_sprintSpeed", baseSprintSpeed);
        currentMultiplier = 1.0f;
        isModified = false;
    }

    private void SetPrivateField(string fieldName, float value)
    {
        var field = typeof(SamplePlayerAnimationController).GetField(
            fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (field != null)
            field.SetValue(animController, value);
        else
            Debug.LogWarning($"[PlayerMovementModifier] Field '{fieldName}' not found!");
    }

    public float GetCurrentMultiplier() => currentMultiplier;
    public bool IsModified() => isModified;
}