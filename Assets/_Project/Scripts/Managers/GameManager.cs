using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Game state manager (singleton).
/// Handles pause, mission state, settings.
/// </summary>
public class GameManager : MonoBehaviour, IInitializable
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    public GameState CurrentState => currentState;
    public bool IsPaused => currentState == GameState.Paused;
    public bool IsInPuzzle => currentState == GameState.InPuzzle;

    private InputReader inputReader;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    public void Initialize(DependencyInjector dependencyInjector)
    {
        inputReader = dependencyInjector.InputReader;

        if (inputReader != null)
        {
            inputReader.onEscapePressed += OnEscapePressed;
        }
        else { 
            Debug.LogError("[GameManager] InputReader is null during initialization!");
        }
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.onEscapePressed -= OnEscapePressed;
    }

    public void EnterPuzzleMode()
    {
        if (currentState == GameState.InPuzzle) return;

        currentState = GameState.InPuzzle;

        if (inputReader != null)
            inputReader.DisableInputs(new[] { "Exit" });

        Debug.Log("[GameManager] Entered Puzzle Mode");
    }

    public void ExitPuzzleMode()
    {
        if (currentState != GameState.InPuzzle) return;

        currentState = GameState.Playing;

        if (inputReader != null)
            inputReader.EnableAllInputs();

        Debug.Log("[GameManager] Exited Puzzle Mode");
    }

    private void OnEscapePressed()
    {
        if (currentState == GameState.InPuzzle)
        {
            HackManager.Instance?.CancelActivePuzzle();
        }
        // TODO: Handle pause menu here
    }

    public void PauseGame()
    {
        currentState = GameState.Paused;
        //Time.timeScale = 0f; never enable!
        Debug.Log("[GameManager] Game paused");
    }

    public void ResumeGame()
    {
        currentState = GameState.Playing;
        //Time.timeScale = 1f; never enable!
        Debug.Log("[GameManager] Game resumed");
    }
}

public enum GameState { Playing, Paused, InPuzzle }