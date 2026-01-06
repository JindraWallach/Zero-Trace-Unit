using UnityEngine;

/// <summary>
/// Spawns taser visual effects (trail + electric impact).
/// SRP: Only handles VFX instantiation, no game logic.
/// </summary>
public class TaserEffectSpawner : MonoBehaviour
{
    [Header("Effect Prefabs")]
    [SerializeField] private GameObject taserTrailPrefab;
    [SerializeField] private GameObject electricImpactPrefab;

    [Header("Timings")]
    [SerializeField] private float trailDuration = 0.3f;
    [SerializeField] private float impactDuration = 1f;

    /// <summary>
    /// Spawn taser trail from enemy to player.
    /// </summary>
    public void SpawnTaserEffect(Vector3 startPos, Vector3 endPos)
    {
        // Trail
        if (taserTrailPrefab != null)
        {
            GameObject trail = Instantiate(taserTrailPrefab, startPos, Quaternion.identity);
            trail.transform.LookAt(endPos);
            Destroy(trail, trailDuration);
        }
        else
        {
            Debug.LogWarning("[TaserEffectSpawner] Trail prefab not assigned!");
        }

        // Impact FX na hráče
        if (electricImpactPrefab != null)
        {
            GameObject impact = Instantiate(electricImpactPrefab, endPos, Quaternion.identity);
            Destroy(impact, impactDuration);
        }
        else
        {
            Debug.LogWarning("[TaserEffectSpawner] Impact prefab not assigned!");
        }
    }
}