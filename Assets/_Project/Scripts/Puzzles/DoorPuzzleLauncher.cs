using System;
using UnityEngine;
public class DoorPuzzleLauncher : MonoBehaviour
{
    [Header("Puzzle")]
    public PuzzleDefinition puzzleDefinition;
    public Transform instantiateParent;

    private GameObject currentInstance;
    private IPuzzle currentPuzzle;

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
