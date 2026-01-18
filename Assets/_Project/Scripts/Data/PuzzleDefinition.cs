using UnityEngine;

/// <summary>
/// ScriptableObject defining puzzle config.
/// </summary>
[CreateAssetMenu(fileName = "PuzzleDefinition", menuName = "Zero Trace/Puzzle Definition")]
public class PuzzleDefinition : ScriptableObject
{
    [Header("Puzzle Config")]
    public GameObject puzzlePrefab;
    public string puzzleName;
    public int difficulty;
}