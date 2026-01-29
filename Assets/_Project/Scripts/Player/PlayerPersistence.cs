using UnityEngine;

/// <summary>
/// Keeps player GameObject alive between scenes using DontDestroyOnLoad.
/// Automatically applies selected class when entering gameplay scene.
/// Handles duplicate player instances correctly.
/// 
/// SETUP:
/// 1. Attach to player prefab in first gameplay scene
/// 2. Player will persist through menu and back
/// 3. No need for separate menu player - this IS the player
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Is this a gameplay scene where class should be applied?")]
    [SerializeField] private bool isGameplayScene = true;

    [Tooltip("Scene names where player should exist (leave empty = all scenes)")]
    [SerializeField] private string[] validSceneNames;

    [Header("References")]
    [SerializeField] private PlayerClassApplier classApplier;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool classAppliedThisSession = false;

    private void Awake()
    {
        // Singleton pattern with duplicate destruction
        if (Instance != null && Instance != this)
        {
            if (debugLog)
                Debug.Log($"[PlayerPersistence] Destroying duplicate player instance: {gameObject.name}");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugLog)
            Debug.Log($"[PlayerPersistence] Player marked as persistent: {gameObject.name}");

        // Auto-find class applier if not assigned
        if (classApplier == null)
            classApplier = GetComponent<PlayerClassApplier>();

        // Subscribe to scene changes
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (debugLog)
            Debug.Log($"[PlayerPersistence] Scene loaded: {scene.name}");

        // Check if this scene is valid for player
        if (!IsValidScene(scene.name))
        {
            if (debugLog)
                Debug.Log($"[PlayerPersistence] Scene '{scene.name}' not in valid scenes list, skipping class application");
            return;
        }

        // Apply class if we're in a gameplay scene and haven't applied yet
        if (ShouldApplyClassInScene(scene.name))
        {
            ApplySelectedClass();
        }
    }

    private bool IsValidScene(string sceneName)
    {
        // If no restrictions, all scenes are valid
        if (validSceneNames == null || validSceneNames.Length == 0)
            return true;

        // Check if current scene is in the valid list
        foreach (string validName in validSceneNames)
        {
            if (sceneName.Equals(validName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool ShouldApplyClassInScene(string sceneName)
    {
        // Only apply in gameplay scenes
        // You can customize this logic - for now, apply in any scene marked as gameplay
        return isGameplayScene;
    }

    /// <summary>
    /// Apply the selected class from PlayerPrefs.
    /// Called automatically when entering gameplay scene.
    /// </summary>
    private void ApplySelectedClass()
    {
        if (classApplier == null)
        {
            Debug.LogWarning("[PlayerPersistence] No PlayerClassApplier found!");
            return;
        }

        // Load selected class from PlayerPrefs
        PlayerClassConfig selectedClass = LoadSelectedClass();

        if (selectedClass == null)
        {
            if (debugLog)
                Debug.Log("[PlayerPersistence] No class selected, using default appearance");
            return;
        }

        // Apply the class
        classApplier.ApplyClass(selectedClass);
        classAppliedThisSession = true;

        if (debugLog)
            Debug.Log($"[PlayerPersistence] Applied class '{selectedClass.className}' to persistent player");
    }

    /// <summary>
    /// Load the selected class from PlayerClassSelector's saved data.
    /// </summary>
    private PlayerClassConfig LoadSelectedClass()
    {
        // Check if class was selected
        if (!PlayerPrefs.HasKey("SelectedClassName"))
        {
            if (debugLog)
                Debug.Log("[PlayerPersistence] No class selected in PlayerPrefs");
            return null;
        }

        string className = PlayerPrefs.GetString("SelectedClassName");

        // Load the class config from Resources
        // This assumes your class configs are in Resources/PlayerClasses/
        PlayerClassConfig loadedClass = Resources.Load<PlayerClassConfig>($"PlayerClasses/{className}");

        if (loadedClass == null)
        {
            Debug.LogWarning($"[PlayerPersistence] Could not load class '{className}' from Resources!");
        }

        return loadedClass;
    }

    /// <summary>
    /// Manually trigger class application (useful for testing).
    /// </summary>
    public void ForceApplyClass()
    {
        ApplySelectedClass();
    }

    /// <summary>
    /// Check if player has applied class this session.
    /// </summary>
    public bool HasAppliedClass() => classAppliedThisSession;
}