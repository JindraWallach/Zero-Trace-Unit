using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleDefinition", menuName = "Puzzles/PuzzleDefinition")]
public class PuzzleDefinition : ScriptableObject
{
    // A prefab that implements IPuzzle (scene prefab or prefab asset)
    public GameObject puzzlePrefab;
}
