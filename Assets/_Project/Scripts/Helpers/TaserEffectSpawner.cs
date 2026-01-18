using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns taser visual effects (trail + electric impact).
/// SRP: Only handles VFX instantiation, no game logic.
/// </summary>
public class TaserEffectSpawner : MonoBehaviour, IInitializable
{
    [Header("Effect Prefabs")]
    [SerializeField] private GameObject taserLinePrefab;
    [SerializeField] private GameObject electricImpactPrefab;

    [Header("Parent Transforms")]
    [SerializeField] private Transform electricEffectsParent; // Pro electric impact efekty
    [SerializeField] private Transform playerChestBone; // Bone hráče pro sledování a electric FX spawn

    [Header("Timings")]
    [SerializeField] private float lineDuration = 0.3f;
    [SerializeField] private float electricImpactDuration = 1f;

    private Transform playerRootTransform;

    public void Initialize(DependencyInjector dependencyInjector)
    {
        playerRootTransform = dependencyInjector.PlayerPosition;

        if (playerRootTransform == null)
            Debug.LogWarning("[TaserEffectSpawner] Player root transform not assigned in DependencyInjector!");

        if (playerChestBone == null)
            Debug.LogWarning("[TaserEffectSpawner] Player chest bone not assigned! Will use root transform with offset.");
    }

    /// <summary>
    /// Spawn taser line from enemy to player chest + electric impact on chest.
    /// </summary>
    public void SpawnTaserEffect(Vector3 EnemyTaserPos, Vector3 initialPlayerChestPosition)
    {
        // Use chest bone if assigned, otherwise fallback to root with offset
        Transform lineTarget = playerChestBone != null ? playerChestBone : playerRootTransform;

        // 1. TASER LINE (enemy → player chest, sleduje chest bone během ragdollu)
        if (taserLinePrefab != null)
        {
            GameObject lineObject = Instantiate(taserLinePrefab, Vector3.zero, Quaternion.identity);

            LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, EnemyTaserPos);
                lineRenderer.SetPosition(1, initialPlayerChestPosition);

                // Start fade-out WITH chest tracking
                StartCoroutine(FadeOutLine(lineRenderer, EnemyTaserPos, lineTarget, lineDuration));
            }
            else
            {
                Debug.LogError("[TaserEffectSpawner] Taser line prefab missing LineRenderer component!");
                Destroy(lineObject);
            }
        }
        else
        {
            Debug.LogWarning("[TaserEffectSpawner] Taser line prefab not assigned!");
        }

        // 2. ELECTRIC IMPACT FX (spawn na chest bone pozici)
        if (electricImpactPrefab != null)
        {
            Vector3 impactPosition = playerChestBone != null ? playerChestBone.position : initialPlayerChestPosition;

            GameObject impactObject = Instantiate(
                electricImpactPrefab,
                impactPosition,
                Quaternion.identity,
                electricEffectsParent
            );

            Destroy(impactObject, electricImpactDuration);
        }
        else
        {
            Debug.LogWarning("[TaserEffectSpawner] Electric impact prefab not assigned!");
        }
    }

    /// <summary>
    /// Gradually fades out line while tracking player chest bone during ragdoll.
    /// </summary>
    private IEnumerator FadeOutLine(LineRenderer lineRenderer, Vector3 enemyPosition, Transform chestTarget, float duration)
    {
        float elapsed = 0f;
        Material lineMaterial = lineRenderer.material;
        Color startColor = lineMaterial.color;

        // Fallback offset if using root transform instead of chest bone
        Vector3 chestOffset = (chestTarget == playerRootTransform && playerChestBone == null) ? Vector3.up * 1f : Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration); // 1 → 0

            // Update alpha (fade out)
            Color newColor = startColor;
            newColor.a = alpha;
            lineMaterial.color = newColor;

            // Update line positions - enemy stays fixed, chest is tracked
            lineRenderer.SetPosition(0, enemyPosition); // Enemy position (fixed)

            if (chestTarget != null)
            {
                lineRenderer.SetPosition(1, chestTarget.position + chestOffset); // Track chest bone
            }

            yield return null;
        }

        // Destroy after fade-out complete
        Destroy(lineRenderer.gameObject);
    }
}