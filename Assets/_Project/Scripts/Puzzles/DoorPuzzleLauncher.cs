using System;
using UnityEngine;

// Attach this to the same GameObject as DoorInteractable.
// Configure the PuzzleDefinition in the inspector per-door.
public class DoorPuzzleLauncher : MonoBehaviour
{
    [Header("Puzzle")]
    public PuzzleDefinition puzzleDefinition;
    // optional parent for instantiated puzzle (e.g., a UI canvas). Leave null to instantiate at root.
    public Transform instantiateParent;

    private GameObject currentInstance;
    private IPuzzle currentPuzzle;

    // Returns true if a puzzle was started.
    public bool TryStartPuzzle(Action onSuccess)
    {
        if (puzzleDefinition == null || puzzleDefinition.puzzlePrefab == null)
            return false;

        if (currentInstance != null)
            return false; // already running

        currentInstance = Instantiate(puzzleDefinition.puzzlePrefab, instantiateParent);
        currentPuzzle = currentInstance.GetComponent<IPuzzle>();
        if (currentPuzzle == null)
        {
            Debug.LogWarning($"Puzzle prefab on '{name}' does not implement IPuzzle.");
            Destroy(currentInstance);
            currentInstance = null;
            return false;
        }

        Action handler = null;
        handler = () =>
        {
            currentPuzzle.OnPuzzleSuccess -= handler;
            try { onSuccess?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
            // cleanup
            try { currentPuzzle.Hide(); } catch { }
            Destroy(currentInstance);
            currentInstance = null;
            currentPuzzle = null;
        };

        currentPuzzle.OnPuzzleSuccess += handler;
        currentPuzzle.Show();
        return true;
    }
}
