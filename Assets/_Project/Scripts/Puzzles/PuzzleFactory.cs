using UnityEngine;

/// <summary>
/// Factory for spawning puzzle prefabs.
/// Handles pooling and puzzle selection logic.
/// </summary>
public class PuzzleFactory : MonoBehaviour
{
    [Header("Puzzle Prefabs")]
    [SerializeField] private GameObject defaultPuzzlePrefab;

    // TODO: Add pooling, difficulty selection

    public GameObject GetPuzzlePrefab(IHackTarget target)
    {
        // For now, return default puzzle
        // Future: select based on target type, difficulty, etc.
        return defaultPuzzlePrefab;
    }
}