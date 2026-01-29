using UnityEngine;
using System;

/// <summary>
/// Applies character class visual customization to player.
/// Supports multiple body parts (head, body, hair, accessories).
/// Scalable - just add more parts to the array.
/// Works with PlayerClassConfig's CharacterPart definitions.
/// </summary>
public class PlayerClassApplier : MonoBehaviour
{
    [Header("Character Parts")]
    [Tooltip("All customizable parts of this character (body, head, hair, etc.)")]
    [SerializeField] private CharacterPartSlot[] characterParts;

    [Header("Optional Movement Integration")]
    [SerializeField] private PlayerMovementModifier movementModifier;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private PlayerClassConfig appliedClass;

    private void Awake()
    {
        // Auto-find movement modifier if not assigned
        if (movementModifier == null)
            movementModifier = GetComponent<PlayerMovementModifier>();

        ValidateCharacterParts();
    }

    /// <summary>
    /// Apply complete character class (visuals + stats).
    /// </summary>
    public void ApplyClass(PlayerClassConfig classConfig)
    {
        if (classConfig == null)
        {
            Debug.LogError("[PlayerClassApplier] Cannot apply null class!");
            return;
        }

        appliedClass = classConfig;

        if (debugLog)
            Debug.Log($"[PlayerClassApplier] Applying class: {classConfig.className}");

        ApplyVisuals(classConfig);
        ApplyMovementStats(classConfig);
    }

    /// <summary>
    /// Apply only visuals (for menu preview without affecting gameplay stats).
    /// </summary>
    public void ApplyVisualsOnly(PlayerClassConfig classConfig)
    {
        if (classConfig == null) return;
        ApplyVisuals(classConfig);
    }

    private void ApplyVisuals(PlayerClassConfig classConfig)
    {
        if (characterParts == null || characterParts.Length == 0)
        {
            Debug.LogWarning("[PlayerClassApplier] No character parts configured!");
            return;
        }

        // Apply each part defined in the class config
        foreach (var configPart in classConfig.characterParts)
        {
            ApplyPart(configPart);
        }

        if (debugLog)
            Debug.Log($"[PlayerClassApplier] Applied {classConfig.characterParts.Length} parts for {classConfig.className}");
    }

    private void ApplyPart(CharacterPart configPart)
    {
        // Find matching slot
        CharacterPartSlot slot = Array.Find(characterParts, s => s.partType == configPart.partType);

        if (slot == null)
        {
            if (debugLog)
                Debug.LogWarning($"[PlayerClassApplier] No slot found for {configPart.partType}");
            return;
        }

        if (slot.renderer == null)
        {
            Debug.LogWarning($"[PlayerClassApplier] Renderer missing for {configPart.partType}!");
            return;
        }

        // Apply mesh if provided
        if (configPart.mesh != null)
        {
            slot.renderer.sharedMesh = configPart.mesh;
            if (debugLog)
                Debug.Log($"[PlayerClassApplier] [{configPart.partType}] Mesh: {configPart.mesh.name}");
        }

        // Apply material if provided
        if (configPart.material != null)
        {
            slot.renderer.material = configPart.material;
            if (debugLog)
                Debug.Log($"[PlayerClassApplier] [{configPart.partType}] Material: {configPart.material.name}");
        }

        // Enable/disable the part based on config
        slot.renderer.gameObject.SetActive(configPart.enabled);
    }

    private void ApplyMovementStats(PlayerClassConfig classConfig)
    {
        if (movementModifier == null)
        {
            if (debugLog)
                Debug.Log("[PlayerClassApplier] No movement modifier - skipping speed modification");
            return;
        }

        movementModifier.ApplySpeedMultiplier(classConfig.movementSpeedMultiplier);
    }

    private void ValidateCharacterParts()
    {
        if (characterParts == null || characterParts.Length == 0)
        {
            Debug.LogWarning("[PlayerClassApplier] No character parts assigned! Add parts in inspector.");
            return;
        }

        // Check for duplicates
        for (int i = 0; i < characterParts.Length; i++)
        {
            for (int j = i + 1; j < characterParts.Length; j++)
            {
                if (characterParts[i].partType == characterParts[j].partType)
                {
                    Debug.LogError($"[PlayerClassApplier] Duplicate part type: {characterParts[i].partType}");
                }
            }
        }
    }

    public PlayerClassConfig GetAppliedClass() => appliedClass;

    /// <summary>
    /// Get reference to a specific part's renderer.
    /// Useful for runtime customization.
    /// </summary>
    public SkinnedMeshRenderer GetPartRenderer(CharacterPartType partType)
    {
        var slot = Array.Find(characterParts, s => s.partType == partType);
        return slot?.renderer;
    }
}

/// <summary>
/// Represents one customizable part slot on the character.
/// </summary>
[Serializable]
public class CharacterPartSlot
{
    [Tooltip("Type of body part (Body, Head, Hair, etc.)")]
    public CharacterPartType partType;

    [Tooltip("SkinnedMeshRenderer for this part")]
    public SkinnedMeshRenderer renderer;
}