using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene loading and transitions (singleton).
/// No DontDestroyOnLoad - recreated per scene for clean state.
/// Uses coroutines for async loading with callbacks.
/// </summary>
public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float minLoadTime = 0.5f; // Minimum time to show loading screen

    [Header("Debug")]
    [SerializeField] private string currentSceneName;
    [SerializeField] private bool isLoading;

    // Events for loading feedback
    public event Action OnSceneLoadStarted;
    public event Action<string> OnSceneLoadCompleted;
    public event Action<float> OnLoadProgress; // 0-1

    private Coroutine loadCoroutine;

    private void Awake()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        // Simple singleton - no DontDestroyOnLoad, new instance per scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// Load scene by name with optional callback.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[SceneManager] Already loading a scene, ignoring request for {sceneName}");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneManager] Scene name is null or empty");
            return;
        }

        loadCoroutine = StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Load scene by build index with optional callback.
    /// </summary>
    public void LoadScene(int sceneIndex, Action onComplete = null)
    {
        if (isLoading)
        {
            Debug.LogWarning($"[SceneManager] Already loading a scene, ignoring request for index {sceneIndex}");
            return;
        }

        if (sceneIndex < 0 || sceneIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneManager] Invalid scene index: {sceneIndex}");
            return;
        }

        loadCoroutine = StartCoroutine(LoadSceneCoroutine(sceneIndex, onComplete));
    }

    /// <summary>
    /// Reload current scene.
    /// </summary>
    public void ReloadCurrentScene()
    {
        LoadScene(currentSceneName);
    }

    /// <summary>
    /// Load next scene in build settings.
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[SceneManager] No next scene available, wrapping to index 0");
            nextIndex = 0;
        }

        LoadScene(nextIndex);
    }

    /// <summary>
    /// Quit application (works in build, logs in editor).
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[SceneManager] Quit requested (Editor mode)");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[SceneManager] Quitting application");
        Application.Quit();
#endif
    }

    // === Coroutines ===

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;
        OnSceneLoadStarted?.Invoke();

        float startTime = Time.realtimeSinceStartup;

        // Start async load
        AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Update progress
        while (operation.progress < 0.9f)
        {
            OnLoadProgress?.Invoke(operation.progress);
            yield return null;
        }

        // Ensure minimum load time for visual consistency
        float elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed < minLoadTime)
            yield return new WaitForSecondsRealtime(minLoadTime - elapsed);

        OnLoadProgress?.Invoke(1f);

        // Activate scene
        operation.allowSceneActivation = true;

        // Wait for scene to actually load
        while (!operation.isDone)
            yield return null;

        currentSceneName = sceneName;
        isLoading = false;

        OnSceneLoadCompleted?.Invoke(sceneName);

        Debug.Log($"[SceneManager] Loaded scene: {sceneName}");
        loadCoroutine = null;
    }

    private IEnumerator LoadSceneCoroutine(int sceneIndex, Action onComplete)
    {
        isLoading = true;
        string sceneName = GetSceneName(sceneIndex);
        OnSceneLoadStarted?.Invoke();

        float startTime = Time.realtimeSinceStartup;

        // Start async load
        AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        // Update progress
        while (operation.progress < 0.9f)
        {
            OnLoadProgress?.Invoke(operation.progress);
            yield return null;
        }

        // Ensure minimum load time
        float elapsed = Time.realtimeSinceStartup - startTime;
        if (elapsed < minLoadTime)
            yield return new WaitForSecondsRealtime(minLoadTime - elapsed);

        OnLoadProgress?.Invoke(1f);

        // Activate scene
        operation.allowSceneActivation = true;

        // Wait for scene to actually load
        while (!operation.isDone)
            yield return null;

        currentSceneName = sceneName;
        isLoading = false;

        OnSceneLoadCompleted?.Invoke(sceneName);
        onComplete?.Invoke();

        Debug.Log($"[SceneManager] Loaded scene: {sceneName}");
        loadCoroutine = null;
    }

    // === Helpers ===

    private string GetSceneName(int index)
    {
        string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(index);
        if (string.IsNullOrEmpty(path))
            return $"Scene_{index}";

        int lastSlash = path.LastIndexOf('/');
        int lastDot = path.LastIndexOf('.');

        if (lastSlash >= 0 && lastDot > lastSlash)
            return path.Substring(lastSlash + 1, lastDot - lastSlash - 1);

        return path;
    }

    public bool IsLoading => isLoading;
    public string CurrentSceneName => currentSceneName;
}