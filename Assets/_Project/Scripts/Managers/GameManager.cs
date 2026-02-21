// Scripts/Managers/GameManager.cs
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

    [Header("UI")]
    [Tooltip("Pause menu root panel")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Tooltip("Objects hidden when pause menu opens or mission completes (e.g. objective popup)")]
    [SerializeField] private GameObject[] hideOnPauseObjects;

    public event Action<GameState> OnGameStateChanged;
    public event Action OnPlayerDied;

    public GameState CurrentState => currentState;
    public bool IsPaused => currentState == GameState.Paused;
    public bool IsInPuzzle => currentState == GameState.InPuzzle;
    public bool IsDead => currentState == GameState.Dead;

    private InputReader inputReader;
    private PlayerDeath playerDeath;
    private TaserEffectSpawner taserEffects;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

    // === PLAYER DEATH ===

    public void OnPlayerCaught(Transform enemyTransform, Vector3 forceDirection, float forceMagnitude)
    {
        if (currentState == GameState.Dead) return;

        ChangeState(GameState.Dead);

        if (inputReader != null)
            inputReader.DisableInputs();

        if (taserEffects != null && playerDeath != null && enemyTransform != null)
            taserEffects.SpawnTaserEffect(enemyTransform.position, playerDeath.transform.position);

        if (playerDeath != null)
            playerDeath.ExecuteDeathWithForce(forceDirection, forceMagnitude, deathSceneReloadDelay);

        OnPlayerDied?.Invoke();
    }

    // === GAME STATE ===

    private void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

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
    }

    public void ExitPuzzleMode()
    {
        if (currentState != GameState.InPuzzle) return;

        ChangeState(GameState.Playing);

        if (inputReader != null)
            inputReader.EnableAllInputs();
    }

    private void OnEscapePressed()
    {
        if (currentState == GameState.Dead || currentState == GameState.MissionComplete)
            return;

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
        if (currentState == GameState.Paused || currentState == GameState.Dead) return;

        ChangeState(GameState.Paused);

        SetPauseMenu(true);
        SetHideOnPauseObjects(false);

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

        SetPauseMenu(false);
        SetHideOnPauseObjects(true);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (inputReader != null)
            inputReader.EnableAllInputs();

        Debug.Log("[GameManager] Game resumed");
    }

    public void OnResumeButtonPressed()
    {
        if (currentState != GameState.Paused) return;
        ResumeGame();
    }

    // === MISSION ===

    /// <summary>
    /// Called by MissionUIHandler when mission is completed.
    /// Owns Time.timeScale = 0 – nobody else writes timeScale directly.
    /// </summary>
    public void OnMissionComplete()
    {
        if (currentState == GameState.MissionComplete) return;

        ChangeState(GameState.MissionComplete);

        if (inputReader != null)
            inputReader.DisableInputs();

        SetHideOnPauseObjects(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;

        Debug.Log("[GameManager] MissionComplete – time frozen.");
    }

    // === SETTINGS ===

    public SettingsManager GetSettings() => SettingsManager.Instance;

    public void ApplySettings()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.ApplySettings();
    }

    // === HELPERS ===

    private void SetPauseMenu(bool visible)
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(visible);
    }

    private void SetHideOnPauseObjects(bool visible)
    {
        foreach (var obj in hideOnPauseObjects)
        {
            if (obj != null) obj.SetActive(visible);
        }
    }
}

public enum GameState
{
    Playing,
    Paused,
    InPuzzle,
    Dead,
    MissionComplete
}