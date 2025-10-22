using System;
using System.Linq;
using UnityEngine;
using Synty.AnimationBaseLocomotion.Samples;
using Synty.AnimationBaseLocomotion.Samples.InputSystem;

public class DoorPuzzleLauncher : MonoBehaviour, IInitializable
{
    [Header("Puzzle")]
    public PuzzleDefinition puzzleDefinition;
    public Transform instantiateParent;

    [Header("Optional: UI / Systems to disable while puzzle runs")]
    [Tooltip("Objects to deactivate while the puzzle is active (e.g. minimap canvas)")]
    public GameObject[] objectsToDisable;

    private InputReader injectedInputReader;
    private SampleCameraController injectedCameraController;

    private GameObject currentInstance;
    private IPuzzle currentPuzzle;

    public void Initialize(DependencyInjector dependencyInjector)
    {
        injectedInputReader = dependencyInjector?.InputReader;
        injectedCameraController = dependencyInjector?.CameraController;
    }

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

        // resolve systems from DI-injected fields
        var inputReaderToDisable = injectedInputReader;
        var cameraControllerToDisable = injectedCameraController;

        // Unlock cursor and disable player input / camera / UI
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (inputReaderToDisable != null)
            inputReaderToDisable.enabled = false;

        if (cameraControllerToDisable != null)
            cameraControllerToDisable.enabled = false;

        if (objectsToDisable != null)
        {
            foreach (var o in objectsToDisable)
            {
                if (o != null)
                    o.SetActive(false);
            }
        }

        Action handler = null;
        handler = () =>
        {
            try { currentPuzzle.OnPuzzleSuccess -= handler; } catch { }
            try { onSuccess?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
            // cleanup
            try { currentPuzzle.Hide(); } catch { }
            Destroy(currentInstance);
            currentInstance = null;
            currentPuzzle = null;

            // restore input/camera/UI/cursor
            if (inputReaderToDisable != null)
                inputReaderToDisable.enabled = true;

            if (cameraControllerToDisable != null)
                cameraControllerToDisable.enabled = true;

            if (objectsToDisable != null)
            {
                foreach (var o in objectsToDisable)
                {
                    if (o != null)
                        o.SetActive(true);
                }
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        };

        Action cancelHandler = null;
        cancelHandler = () =>
        {
            try { currentPuzzle.OnPuzzleCancelled -= cancelHandler; } catch { }
            // cleanup without calling success callback (treat as failure/cancel)
            try { currentPuzzle.Hide(); } catch { }
            Destroy(currentInstance);
            currentInstance = null;
            currentPuzzle = null;

            if (inputReaderToDisable != null)
                inputReaderToDisable.enabled = true;

            if (cameraControllerToDisable != null)
                cameraControllerToDisable.enabled = true;

            if (objectsToDisable != null)
            {
                foreach (var o in objectsToDisable)
                {
                    if (o != null)
                        o.SetActive(true);
                }
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            Debug.Log("Puzzle cancelled via ESC.");
        };

        currentPuzzle.OnPuzzleSuccess += handler;
        currentPuzzle.OnPuzzleCancelled += cancelHandler;

        currentPuzzle.Show();
        return true;
    }
}
