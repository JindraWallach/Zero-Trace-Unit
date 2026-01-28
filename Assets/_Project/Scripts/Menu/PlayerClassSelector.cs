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

    private void Awake()
    {
        DebugLog($"Awake start. defaultIndex={defaultClassIndex}, availableCount={availableClasses?.Length ?? 0}");

        ValidateClassList();
        LoadSavedSelection();
        DebugLog($"Awake end. currentIndex={currentClassIndex}");
    }

    private void Start()
    {
        // Intentionally left empty — initialization moved to Awake to ensure predictable ordering.
        DebugLog("Start called.");
    }

    public void SelectNextClass()
    {
        if (availableClasses == null || availableClasses.Length == 0)
        {
            DebugLog("SelectNextClass called but no available classes.");
            return;
        }

        int old = currentClassIndex;
        currentClassIndex = (currentClassIndex + 1) % availableClasses.Length;
        DebugLog($"SelectNextClass: {old} -> {currentClassIndex}");
        OnClassChanged();
    }

    public void SelectPreviousClass()
    {
        if (availableClasses == null || availableClasses.Length == 0)
        {
            DebugLog("SelectPreviousClass called but no available classes.");
            return;
        }                       

        int old = currentClassIndex;
        currentClassIndex--;
        if (currentClassIndex < 0)
            currentClassIndex = availableClasses.Length - 1;
        DebugLog($"SelectPreviousClass: {old} -> {currentClassIndex}");
        OnClassChanged();
    }

    public void SelectClass(int index)
    {
        if (availableClasses == null || index < 0 || index >= availableClasses.Length)
        {
            DebugLog($"SelectClass: Invalid class index: {index}");
            Debug.LogWarning($"[PlayerClassSelector] Invalid class index: {index}");
            return;
        }

        int old = currentClassIndex;
        currentClassIndex = index;
        DebugLog($"SelectClass: {old} -> {currentClassIndex}");
        OnClassChanged();
    }

    public void ConfirmSelection()
    {
        if (CurrentClass == null)
        {
            DebugLog("ConfirmSelection called but CurrentClass is null.");
            Debug.LogError("[PlayerClassSelector] No class selected!");
            return;
        }

        SaveSelection();

        if (debugLog)
            Debug.Log($"[PlayerClassSelector] Confirmed: {CurrentClass.className}");
    }

    private void SaveSelection()
    {
        DebugLog($"Saving selection index={currentClassIndex}");
        PlayerPrefs.SetInt("SelectedClassIndex", currentClassIndex);
        PlayerPrefs.Save();
    }

    private void LoadSavedSelection()
    {
        if (availableClasses == null || availableClasses.Length == 0)
        {
            DebugLog("LoadSavedSelection: no available classes, setting currentClassIndex = 0");
            currentClassIndex = 0;
            return;
        }

        if (PlayerPrefs.HasKey("SelectedClassIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("SelectedClassIndex");
            DebugLog($"LoadSavedSelection: found savedIndex={savedIndex}");
            if (savedIndex >= 0 && savedIndex < availableClasses.Length)
            {
                currentClassIndex = savedIndex;
                DebugLog($"LoadSavedSelection: using savedIndex -> currentClassIndex={currentClassIndex}");
                OnClassChanged();
                return;
            }
            DebugLog($"LoadSavedSelection: savedIndex out of range (0..{availableClasses.Length - 1}), ignoring");
        }
        else
        {
            DebugLog("LoadSavedSelection: no saved index in PlayerPrefs");
        }

        currentClassIndex = Mathf.Clamp(defaultClassIndex, 0, availableClasses.Length - 1);
        DebugLog($"LoadSavedSelection: using defaultIndex -> currentClassIndex={currentClassIndex}");
        OnClassChanged();
    }

    private void OnClassChanged()
    {
        if (CurrentClass != null && debugLog)
            Debug.Log($"[PlayerClassSelector] Selected: {CurrentClass.className} (index={currentClassIndex})");
        else
            DebugLog($"OnClassChanged: CurrentClass is null (index={currentClassIndex})");
    }

    private void ValidateClassList()
    {
        if (availableClasses == null || availableClasses.Length == 0)
        {
            Debug.LogError("[PlayerClassSelector] No classes assigned!");
            return;
        }

        DebugLog($"ValidateClassList: found {availableClasses.Length} classes.");
        for (int i = 0; i < availableClasses.Length; i++)
        {
            if (availableClasses[i] == null)
                Debug.LogWarning($"[PlayerClassSelector] Class at index {i} is null!");
            else
                DebugLog($"ValidateClassList: index {i} = {availableClasses[i].className}");
        }
    }

    public PlayerClassConfig[] GetAvailableClasses() => availableClasses;

    private void DebugLog(string message)
    {
        if (!debugLog) return;
        Debug.Log($"[PlayerClassSelector] {message}");
    }
}