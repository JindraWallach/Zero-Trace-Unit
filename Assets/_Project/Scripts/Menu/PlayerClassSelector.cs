using UnityEngine;

public class PlayerClassSelector : MonoBehaviour
{
    [Header("Available Classes")]
    [SerializeField] private PlayerClassConfig[] availableClasses;
    [SerializeField] private int defaultClassIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

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

    public void SelectNextClass()
    {
        if (availableClasses == null || availableClasses.Length == 0) return;
        currentClassIndex = (currentClassIndex + 1) % availableClasses.Length;
        OnClassChanged();
    }

    public void SelectPreviousClass()
    {
        if (availableClasses == null || availableClasses.Length == 0) return;
        currentClassIndex--;
        if (currentClassIndex < 0)
            currentClassIndex = availableClasses.Length - 1;
        OnClassChanged();
    }

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

    public void ConfirmSelection()
    {
        if (CurrentClass == null)
        {
            Debug.LogError("[PlayerClassSelector] No class selected!");
            return;
        }

        SaveSelection();

        if (debugLog)
            Debug.Log($"[PlayerClassSelector] Confirmed: {CurrentClass.className}");

        // Notify listeners (ClassSelectionUI will handle player spawn)
    }

    private void SaveSelection()
    {
        PlayerPrefs.SetInt("SelectedClassIndex", currentClassIndex);
        PlayerPrefs.Save();
    }

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

        currentClassIndex = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Length - 1);
        OnClassChanged();
    }

    private void OnClassChanged()
    {
        if (CurrentClass != null && debugLog)
            Debug.Log($"[PlayerClassSelector] Selected: {CurrentClass.className}");
    }

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
                Debug.LogWarning($"[PlayerClassSelector] Class at index {i} is null!");
        }
    }

    public PlayerClassConfig[] GetAvailableClasses() => availableClasses;
}