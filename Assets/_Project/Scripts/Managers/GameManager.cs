using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System;
using UnityEngine;

/// <summary>
/// Game state manager (singleton per scene).
/// Coordinates game flow, delegates execution to specialized systems.
/// NOT DontDestroyOnLoad - recreated per scene for clean state.
/// </summary>
public class GameManager : MonoBehaviour, IInitializable
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Death Handling")]
    [SerializeField] private float deathSceneReloadDelay = 2f;

    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;



    // Events for game state changes
    public event Action<GameState> OnGameStateChanged;
    public event Action OnPlayerDied; // NEW: Notify listeners about player death

    public GameState CurrentState => currentState;
    public bool IsPaused => currentState == GameState.Paused;
    public bool IsInPuzzle => currentState == GameState.InPuzzle;
    public bool IsDead => currentState == GameState.Dead;

    private InputReader inputReader;
    private PlayerDeath playerDeath;
    private TaserEffectSpawner taserEffects;

    private void Awake()
    {
        // NO DontDestroyOnLoad - we want fresh state per scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        playerDeath = FindFirstObjectByType<PlayerDeath>();
        taserEffects = FindFirstObjectByType<TaserEffectSpawner>();
    }

    public void Initialize(DependencyInjector dependencyInjector)
    {
        inputReader = dependencyInjector.InputReader;

        if (inputReader != null)
            inputReader.onEscapePressed += OnEscapePressed;
        else
            Debug.LogError("[GameManager] InputReader is null during initialization!");

        // Find PlayerDeath component in scene
        playerDeath = FindFirstObjectByType<PlayerDeath>();
        if (playerDeath == null)
            Debug.LogWarning("[GameManager] PlayerDeath component not found in scene!");
    }

    private void OnDestroy()
    {
        if (inputReader != null)
            inputReader.onEscapePressed -= OnEscapePressed;

        if (Instance == this)
            Instance = null;
    }

    // === PLAYER DEATH HANDLING ===

    /// <summary>
    /// Called by EnemyCatchState when player is caught.
    /// Delegates to PlayerDeath for execution.
    /// </summary>
    public void OnPlayerCaught(Transform enemyTransform, Vector3 forceDirection, float forceMagnitude)
    {
        if (currentState == GameState.Dead) return;

        ChangeState(GameState.Dead);

        if (inputReader != null)
            inputReader.DisableInputs();

        // 1. Spawn vizuální efekty HNED (taser trail/electric FX)
        if (taserEffects != null && playerDeath != null && enemyTransform != null)
        {
            Vector3 taserPoint = enemyTransform.position;
            Vector3 playerChest = playerDeath.transform.position;

            // TaserEffectSpawner automaticky použije playerTransform z DI
            taserEffects.SpawnTaserEffect(taserPoint, playerChest);
        }

        // 2. Execute death s force
        if (playerDeath != null)
        {
            playerDeath.ExecuteDeathWithForce(forceDirection, forceMagnitude, deathSceneReloadDelay);
        }

        OnPlayerDied?.Invoke();
    }

    // === GAME STATE MANAGEMENT ===

    private void ChangeState(GameState newState)
    {
        if (currentState == newState)
            return;

        GameState oldState = currentState;
        currentState = newState;

        Debug.Log($"[GameManager] State: {oldState} → {newState}");
        OnGameStateChanged?.Invoke(newState);
    }

    public void EnterPuzzleMode()
    {
        if (currentState == GameState.InPuzzle) return;

        ChangeState(GameState.InPuzzle);

        if (inputReader != null)
            inputReader.DisableInputs(new[] { "Exit" });

        Debug.Log("[GameManager] Entered Puzzle Mode");
    }

    public void ExitPuzzleMode()
    {
        if (currentState != GameState.InPuzzle) return;

        ChangeState(GameState.Playing);

        if (inputReader != null)
            inputReader.EnableAllInputs();

        Debug.Log("[GameManager] Exited Puzzle Mode");
    }

    private void OnEscapePressed()
    {
        if (currentState == GameState.Dead)
            return; // No pause during death

        if (currentState == GameState.InPuzzle)
        {
            HackManager.Instance?.CancelActivePuzzle();
            return;
        }

        if (currentState == GameState.Paused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (currentState == GameState.Paused || currentState == GameState.Dead)
            return;

        ChangeState(GameState.Paused);

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

        ChangeState(GameState.Playing);

        UIManager.Instance?.HidePauseMenu();

        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (inputReader != null)
            inputReader.EnableAllInputs();

        Debug.Log("[GameManager] Game resumed");
    }

    // === SETTINGS INTEGRATION ===

    public SettingsManager GetSettings()
    {
        return SettingsManager.Instance;
    }

    public void ApplySettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplySettings();
            Debug.Log("[GameManager] Applied settings");
        }
    }
}

public enum GameState
{
    Playing,
    Paused,
    InPuzzle,
    Dead // NEW: Death state
}