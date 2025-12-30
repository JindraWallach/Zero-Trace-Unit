using UnityEngine;

/// <summary>
/// Factory for spawning puzzle prefabs.
/// Handles puzzle selection based on target configuration.
/// </summary>
public class PuzzleFactory : MonoBehaviour
{
    [Header("Fallback")]
    [SerializeField] private GameObject defaultPuzzlePrefab;

    /// <summary>
    /// Get puzzle prefab for target.
    /// Priority: Target's PuzzleDefinition > Default fallback
    /// </summary>
    public GameObject GetPuzzlePrefab(IHackTarget target)
    {
        // Try to get PuzzleDefinition from HackableDoor
        if (target is HackableDoor hackableDoor)
        {
            var puzzleDef = hackableDoor.GetPuzzleDefinition();

            if (puzzleDef != null && puzzleDef.puzzlePrefab != null)
            {
                Debug.Log($"[PuzzleFactory] Using puzzle from definition: {puzzleDef.puzzleName}");
                return puzzleDef.puzzlePrefab;
            }
        }

        // Fallback to default
        if (defaultPuzzlePrefab != null)
        {
            Debug.Log("[PuzzleFactory] Using default puzzle prefab");
            return defaultPuzzlePrefab;
        }

        Debug.LogError("[PuzzleFactory] No puzzle prefab available!");
        return null;
    }
}