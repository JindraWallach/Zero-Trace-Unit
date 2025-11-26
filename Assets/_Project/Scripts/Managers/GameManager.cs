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

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;

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

        if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (currentState == GameState.Paused) return;

        currentState = GameState.Paused;

        // Use UIManager to show pause menu
        UIManager.Instance?.ShowPauseMenu();

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (inputReader != null)
            inputReader.DisableInputs(new[] { "Exit" });

        Debug.Log("[GameManager] Game paused");
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;

        // Use UIManager to hide pause menu
        UIManager.Instance?.HidePauseMenu();

        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (inputReader != null)
            inputReader.EnableAllInputs();

        Debug.Log("[GameManager] Game resumed");
    }
}

public enum GameState { Playing, Paused, InPuzzle }