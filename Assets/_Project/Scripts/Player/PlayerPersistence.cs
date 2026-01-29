using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps player alive between scenes and applies selected PlayerClassConfig
/// when entering gameplay scenes.
/// Safe for DontDestroyOnLoad usage (no scene references).
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool persistBetweenScenes = true;

    [Tooltip("Names of scenes where class should be applied (empty = all scenes)")]
    [SerializeField] private string[] validSceneNames;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private bool classAppliedThisSession = false;

    private void Awake()
    {
        if (persistBetweenScenes)
        {
            if (Instance != null && Instance != this)
            {
                if (debugLog)
                    Debug.Log("[PlayerPersistence] Duplicate instance destroyed.");

                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            if (debugLog)
                Debug.Log("[PlayerPersistence] Player marked as DontDestroyOnLoad.");
        }
        else
        {
            Instance = this;
        }

        // ✅ explicit Unity SceneManager
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // ✅ explicit Unity SceneManager
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (debugLog)
            Debug.Log($"[PlayerPersistence] Scene loaded: {scene.name}");

        if (!IsValidScene(scene.name))
        {
            if (debugLog)
                Debug.Log("[PlayerPersistence] Scene not valid for class application.");
            return;
        }

        ApplySelectedClass();
    }

    private bool IsValidScene(string sceneName)
    {
        if (validSceneNames == null || validSceneNames.Length == 0)
            return true;

        foreach (string name in validSceneNames)
        {
            if (sceneName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void ApplySelectedClass()
    {
        PlayerClassConfig selectedClass = LoadSelectedClass();

        if (selectedClass == null)
        {
            if (debugLog)
                Debug.Log("[PlayerPersistence] No selected class found.");
            return;
        }

        PlayerClassApplier applier = Object.FindFirstObjectByType<PlayerClassApplier>();

        if (applier == null)
        {
            Debug.LogWarning("[PlayerPersistence] PlayerClassApplier not found in scene!");
            return;
        }

        applier.ApplyClass(selectedClass);
        classAppliedThisSession = true;

        if (debugLog)
            Debug.Log($"[PlayerPersistence] Applied class '{selectedClass.className}' to '{applier.gameObject.name}'.");
    }

    private PlayerClassConfig LoadSelectedClass()
    {
        if (!PlayerPrefs.HasKey("SelectedClassName"))
        {
            if (debugLog)
                Debug.Log("[PlayerPersistence] No SelectedClassName in PlayerPrefs.");
            return null;
        }

        string className = PlayerPrefs.GetString("SelectedClassName");

        PlayerClassConfig loadedClass = Resources.Load<PlayerClassConfig>($"PlayerClasses/{className}");

        if (loadedClass == null)
        {
            Debug.LogWarning($"[PlayerPersistence] Could not load class '{className}' from Resources.");
        }

        return loadedClass;
    }

    public void ForceApplyClass()
    {
        ApplySelectedClass();
    }

    public bool HasAppliedClass() => classAppliedThisSession;
}
