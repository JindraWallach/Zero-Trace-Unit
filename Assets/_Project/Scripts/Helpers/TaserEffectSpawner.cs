using System.Collections;
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
            StartCoroutine(AnimateTrail(trail, startPos, endPos));
        }
        else
        {
            Debug.LogWarning("[TaserEffectSpawner] Trail prefab not assigned!");
        }

        // Impact FX na hráče
        if (electricImpactPrefab != null)
        {
            GameObject impact = Instantiate(electricImpactPrefab, endPos, Quaternion.identity, gameObject.transform);
            Destroy(impact, impactDuration);
        }
        else
        {
            Debug.LogWarning("[TaserEffectSpawner] Impact prefab not assigned!");
        }
    }

    private IEnumerator AnimateTrail(GameObject trail, Vector3 start, Vector3 end)
    {
        float elapsed = 0f;

        while (elapsed < trailDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / trailDuration;
            trail.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        Destroy(trail, 0.5f); // extra čas na dofadnutí trailu
    }
}