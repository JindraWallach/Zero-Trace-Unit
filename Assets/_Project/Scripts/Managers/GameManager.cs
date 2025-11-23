using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

/// <summary>
/// Game state manager (singleton).
/// Handles pause, mission state, settings integration.
/// Updated to work with SettingsManager.
/// </summary>
public class GameManager : MonoBehaviour, IInitializable
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Settings")]
    [SerializeField] private bool initializeSettingsOnStart = true;

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

        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(DependencyInjector dependencyInjector)
    {
        inputReader = dependencyInjector.InputReader;

        if (inputReader != null)
        {
            inputReader.onEscapePressed += OnEscapePressed;
        }
        else
        {
            Debug.LogError("[GameManager] InputReader is null during initialization!");
        }
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.onEscapePressed -= OnEscapePressed;
    }

    /// <summary>
    /// Get reference to SettingsManager (convenience method).
    /// </summary>
    public SettingsManager GetSettings()
    {
        return SettingsManager.Instance;
    }

    /// <summary>
    /// Apply all settings (useful after loading save game).
    /// </summary>
    public void ApplySettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplySettings();
            Debug.Log("[GameManager] Applied settings");
        }
    }

    // === GAME STATE MANAGEMENT ===

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

    public void OnPlayerCaught()
    {
        if (currentState == GameState.InPuzzle) return; // Ignore during puzzle

        currentState = GameState.Paused; // Or GameState.Dead

        if (inputReader != null)
            inputReader.DisableInputs();

        Debug.Log("[GameManager] Player caught by enemy!");

        // TODO: Show death screen UI
        // TODO: Delayed scene reload
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
        // NOTE: Never use Time.timeScale = 0 for pause!
        // Use input disabling instead
        Debug.Log("[GameManager] Game paused");
    }

    public void ResumeGame()
    {
        currentState = GameState.Playing;
        // NOTE: Never use Time.timeScale = 1 for resume!
        Debug.Log("[GameManager] Game resumed");
    }

    // === PAUSE MENU INTEGRATION ===

    /// <summary>
    /// Show pause menu (integrate with your UI system).
    /// </summary>
    public void ShowPauseMenu()
    {
        PauseGame();
        // TODO: Show pause menu UI
        // UIManager.Instance?.ShowPauseMenu();
    }

    /// <summary>
    /// Hide pause menu and resume.
    /// </summary>
    public void HidePauseMenu()
    {
        ResumeGame();
        // TODO: Hide pause menu UI
        // UIManager.Instance?.HidePauseMenu();
    }

    /// <summary>
    /// Show settings menu from pause menu.
    /// </summary>
    public void ShowSettingsMenu()
    {
        // TODO: Show settings UI
        // UIManager.Instance?.ShowSettingsMenu();
    }
}

public enum GameState { Playing, Paused, InPuzzle }