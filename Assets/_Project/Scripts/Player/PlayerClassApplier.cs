using UnityEngine;

public class PlayerClassApplier : MonoBehaviour
{
    [Header("Player Components (Auto-found)")]
    [SerializeField] private SkinnedMeshRenderer bodyRenderer;
    [SerializeField] private PlayerMovementModifier movementModifier;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private PlayerClassConfig appliedClass;

    private void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (movementModifier == null)
            movementModifier = GetComponent<PlayerMovementModifier>();
    }

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

        ApplySkin(classConfig);
        ApplyMovementStats(classConfig);
    }

    private void ApplySkin(PlayerClassConfig classConfig)
    {
        if (bodyRenderer == null)
        {
            Debug.LogWarning("[PlayerClassApplier] SkinnedMeshRenderer not found!");
            return;
        }

        if (classConfig.characterMesh != null)
        {
            bodyRenderer.sharedMesh = classConfig.characterMesh;
            if (debugLog)
                Debug.Log($"[PlayerClassApplier] Mesh swapped: {classConfig.characterMesh.name}");
        }

        if (classConfig.characterMaterial != null)
        {
            bodyRenderer.material = classConfig.characterMaterial;
            if (debugLog)
                Debug.Log($"[PlayerClassApplier] Material swapped: {classConfig.characterMaterial.name}");
        }
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

    public PlayerClassConfig GetAppliedClass() => appliedClass;
}