using UnityEngine;

/// <summary>
/// Manages class selection logic (browsing, confirming).
/// Saves selection to PlayerPrefs for persistence across scenes.
/// </summary>
public class PlayerClassSelector : MonoBehaviour
{
    [Header("Available Classes")]
    [Tooltip("All available player classes (set in Inspector)")]
    [SerializeField] private PlayerClassConfig[] availableClasses;

    [Header("Settings")]
    [Tooltip("Default class index on first load")]
    [SerializeField] private int defaultClassIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private int currentIndex;
    private bool hasConfirmed = false;

    public PlayerClassConfig CurrentClass =>
        (availableClasses != null && currentIndex >= 0 && currentIndex < availableClasses.Length)
            ? availableClasses[currentIndex]
            : null;

    private void Start()
    {
        ValidateClasses();
        LoadLastSelection();
    }

    private void ValidateClasses()
    {
        if (availableClasses == null || availableClasses.Length == 0)
        {
            Debug.LogError("[PlayerClassSelector] No classes assigned! Add classes in Inspector.");
            enabled = false;
            return;
        }

        // Remove null entries
        availableClasses = System.Array.FindAll(availableClasses, c => c != null);

        if (availableClasses.Length == 0)
        {
            Debug.LogError("[PlayerClassSelector] All class slots are null!");
            enabled = false;
        }
    }

    private void LoadLastSelection()
    {
        // Try to load previously selected class
        if (PlayerPrefs.HasKey("SelectedClassIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedClassIndex");
            currentIndex = Mathf.Clamp(savedIndex, 0, availableClasses.Length - 1);

            if (debugLog)
                Debug.Log($"[PlayerClassSelector] Loaded saved selection: {CurrentClass?.className}");
        }
        else
        {
            currentIndex = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Length - 1);
            if (debugLog)
                Debug.Log($"[PlayerClassSelector] Using default class: {CurrentClass?.className}");
        }
    }

    public void SelectNextClass()
    {
        if (availableClasses == null || availableClasses.Length == 0) return;

        currentIndex = (currentIndex + 1) % availableClasses.Length;
        hasConfirmed = false;

        if (debugLog)
            Debug.Log($"[PlayerClassSelector] Selected: {CurrentClass?.className} ({currentIndex + 1}/{availableClasses.Length})");
    }

    public void SelectPreviousClass()
    {
        if (availableClasses == null || availableClasses.Length == 0) return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = availableClasses.Length - 1;

        hasConfirmed = false;

        if (debugLog)
            Debug.Log($"[PlayerClassSelector] Selected: {CurrentClass?.className} ({currentIndex + 1}/{availableClasses.Length})");
    }

    /// <summary>
    /// Confirm selection and save to PlayerPrefs.
    /// This is what persists the choice across scenes.
    /// </summary>
    public void ConfirmSelection()
    {
        if (CurrentClass == null)
        {
            Debug.LogError("[PlayerClassSelector] Cannot confirm - no class selected!");
            return;
        }

        hasConfirmed = true;

        // Save index
        PlayerPrefs.SetInt("SelectedClassIndex", currentIndex);

        // Save class name (used by PlayerPersistence to load the actual config)
        PlayerPrefs.SetString("SelectedClassName", CurrentClass.name);

        PlayerPrefs.Save();

        if (debugLog)
            Debug.Log($"[PlayerClassSelector] ✓ Confirmed and saved: {CurrentClass.className}");
    }

    /// <summary>
    /// Get all available classes (useful for UI population).
    /// </summary>
    public PlayerClassConfig[] GetAllClasses() => availableClasses;

    /// <summary>
    /// Check if current selection has been confirmed.
    /// </summary>
    public bool IsConfirmed() => hasConfirmed;

    /// <summary>
    /// Get current selection index.
    /// </summary>
    public int GetCurrentIndex() => currentIndex;

    /// <summary>
    /// Set class by index (useful for direct selection).
    /// </summary>
    public void SetClassByIndex(int index)
    {
        if (index < 0 || index >= availableClasses.Length)
        {
            Debug.LogWarning($"[PlayerClassSelector] Invalid index: {index}");
            return;
        }

        currentIndex = index;
        hasConfirmed = false;

        if (debugLog)
            Debug.Log($"[PlayerClassSelector] Set to: {CurrentClass?.className}");
    }

    /// <summary>
    /// Clear saved selection (reset to default).
    /// </summary>
    public void ClearSavedSelection()
    {
        PlayerPrefs.DeleteKey("SelectedClassIndex");
        PlayerPrefs.DeleteKey("SelectedClassName");
        PlayerPrefs.Save();

        currentIndex = defaultClassIndex;
        hasConfirmed = false;

        if (debugLog)
            Debug.Log("[PlayerClassSelector] Cleared saved selection");
    }
}