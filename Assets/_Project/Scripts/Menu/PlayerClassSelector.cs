using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles class selection logic in menu.
/// SRP: Selection state management and scene transition only.
/// Attach to menu scene manager.
/// </summary>
public class PlayerClassSelector : MonoBehaviour
{
    [Header("Available Classes")]
    [Tooltip("List of all available player classes")]
    [SerializeField] private PlayerClassConfig[] availableClasses;

    [Header("Default Selection")]
    [Tooltip("Index of default class (0 = first)")]
    [SerializeField] private int defaultClassIndex = 0;

    [Header("Scene Management")]
    [Tooltip("Name of game scene to load")]
    [SerializeField] private string gameSceneName = "GameScene";

    private int currentClassIndex;

    public PlayerClassConfig CurrentClass =>
        availableClasses != null && currentClassIndex >= 0 && currentClassIndex < availableClasses.Length
            ? availableClasses[currentClassIndex]
            : null;

    public int CurrentClassIndex => currentClassIndex;
    public int ClassCount => availableClasses?.Length ?? 0;

    private void Start()
    {
        ValidateClassList();
        LoadSavedSelection();
    }

    /// <summary>
    /// Select next class (right arrow).
    /// </summary>
    public void SelectNextClass()
    {
        if (availableClasses == null || availableClasses.Length == 0)
            return;

        currentClassIndex = (currentClassIndex + 1) % availableClasses.Length;
        OnClassChanged();
    }

    /// <summary>
    /// Select previous class (left arrow).
    /// </summary>
    public void SelectPreviousClass()
    {
        if (availableClasses == null || availableClasses.Length == 0)
            return;

        currentClassIndex--;
        if (currentClassIndex < 0)
            currentClassIndex = availableClasses.Length - 1;

        OnClassChanged();
    }

    /// <summary>
    /// Select class by index.
    /// </summary>
    public void SelectClass(int index)
    {
        if (availableClasses == null || index < 0 || index >= availableClasses.Length)
        {
            Debug.LogWarning($"[PlayerClassSelector] Invalid class index: {index}");
            return;
        }

        currentClassIndex = index;
        OnClassChanged();
    }

    /// <summary>
    /// Confirm selection and start game.
    /// </summary>
    public void ConfirmSelection()
    {
        if (CurrentClass == null)
        {
            Debug.LogError("[PlayerClassSelector] No class selected!");
            return;
        }

        // Set selected class in manager
        if (PlayerClassManager.Instance != null)
        {
            PlayerClassManager.Instance.SetSelectedClass(CurrentClass);
        }
        else
        {
            Debug.LogError("[PlayerClassSelector] PlayerClassManager not found!");
            return;
        }

        // Save selection
        SaveSelection();

        // Load game scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            if (SceneManager.Instance != null)
            {
                SceneManager.Instance.LoadScene(gameSceneName);
            }
            else
            {
                // Fallback to Unity SceneManager
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
            }
        }
        else
        {
            Debug.LogError("[PlayerClassSelector] Game scene name not set!");
        }
    }

    /// <summary>
    /// Save selection to PlayerPrefs.
    /// </summary>
    private void SaveSelection()
    {
        PlayerPrefs.SetInt("SelectedClassIndex", currentClassIndex);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load saved selection from PlayerPrefs.
    /// </summary>
    private void LoadSavedSelection()
    {
        if (PlayerPrefs.HasKey("SelectedClassIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedClassIndex");
            if (savedIndex >= 0 && savedIndex < availableClasses.Length)
            {
                currentClassIndex = savedIndex;
                OnClassChanged();
                return;
            }
        }

        // Use default
        currentClassIndex = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Length - 1);
        OnClassChanged();
    }

    /// <summary>
    /// Called when class changes (for UI updates).
    /// </summary>
    private void OnClassChanged()
    {
        if (CurrentClass != null)
        {
            Debug.Log($"[PlayerClassSelector] Selected: {CurrentClass.className}");
        }
    }

    /// <summary>
    /// Validate class list on start.
    /// </summary>
    private void ValidateClassList()
    {
        if (availableClasses == null || availableClasses.Length == 0)
        {
            Debug.LogError("[PlayerClassSelector] No classes assigned!");
            return;
        }

        for (int i = 0; i < availableClasses.Length; i++)
        {
            if (availableClasses[i] == null)
            {
                Debug.LogWarning($"[PlayerClassSelector] Class at index {i} is null!");
            }
        }
    }

    /// <summary>
    /// Get all available classes (for UI).
    /// </summary>
    public PlayerClassConfig[] GetAvailableClasses()
    {
        return availableClasses;
    }
}