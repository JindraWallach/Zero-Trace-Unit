using UnityEngine;
using System;

public class HackManager : MonoBehaviour
{
    public static HackManager Instance;

    private IPuzzle activePuzzle;
    private GameObject activePuzzleGO;

    [Header("Settings")]
    public Transform puzzleParent;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RequestHack(GameObject puzzlePrefab, Action onSuccess = null, Action onFail = null)
    {
        if (activePuzzle != null)
        {
            Debug.LogWarning("Puzzle already active");
            return;
        }

        activePuzzleGO = Instantiate(puzzlePrefab, puzzleParent);
        activePuzzle = activePuzzleGO.GetComponent<IPuzzle>();

        if (activePuzzle != null)
        {
            activePuzzle.OnPuzzleSuccess += () => {
                onSuccess?.Invoke();
                CleanupPuzzle();
            };
            activePuzzle.OnPuzzleCancelled += () => {
                onFail?.Invoke();
                CleanupPuzzle();
            };
            activePuzzle.Show();
        }
    }

    private void CleanupPuzzle()
    {
        if (activePuzzleGO != null)
            Destroy(activePuzzleGO);
        activePuzzle = null;
        activePuzzleGO = null;
    }
}