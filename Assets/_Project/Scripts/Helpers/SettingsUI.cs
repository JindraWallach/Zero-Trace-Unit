using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller for settings menu.
/// Binds UI elements to SettingsManager.
/// Updates UI when settings change.
/// SRP: Only handles UI presentation, no business logic.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Quality Settings")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Volume Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;

    [Header("Resolution Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Fullscreen Settings")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    private SettingsManager settings;
    private bool isInitialized;

    private void Start()
    {
        settings = SettingsManager.Instance;

        if (settings == null)
        {
            Debug.LogError("[SettingsUI] SettingsManager instance not found!");
            enabled = false;
            return;
        }

        InitializeUI();
        BindEvents();
        UpdateUIFromSettings();

        isInitialized = true;
    }

    private void OnEnable()
    {
        if (isInitialized && settings != null)
            UpdateUIFromSettings();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    // === INITIALIZATION ===

    private void InitializeUI()
    {
        // Populate quality dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(settings.GetQualityLevels()));
        }

        // Populate resolution dropdown
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var resOptions = new System.Collections.Generic.List<string>();

            foreach (var res in settings.GetAvailableResolutions())
            {
                resOptions.Add(settings.GetResolutionString(res));
            }

            resolutionDropdown.AddOptions(resOptions);
        }

        // Setup volume slider (1-100, whole numbers)
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 100f;
            volumeSlider.wholeNumbers = true;
        }
    }

    // === EVENT BINDING ===

    private void BindEvents()
    {
        // UI -> Settings
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityDropdownChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);

        // Buttons
        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        // Settings -> UI (for external changes)
        if (settings != null)
        {
            settings.OnQualityChanged += OnSettingsQualityChanged;
            settings.OnVolumeChanged += OnSettingsVolumeChanged;
            settings.OnResolutionChanged += OnSettingsResolutionChanged;
            settings.OnFullscreenChanged += OnSettingsFullscreenChanged;
        }
    }

    private void UnbindEvents()
    {
        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.RemoveListener(OnQualityDropdownChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSliderChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionDropdownChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(OnResetClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);

        if (settings != null)
        {
            settings.OnQualityChanged -= OnSettingsQualityChanged;
            settings.OnVolumeChanged -= OnSettingsVolumeChanged;
            settings.OnResolutionChanged -= OnSettingsResolutionChanged;
            settings.OnFullscreenChanged -= OnSettingsFullscreenChanged;
        }
    }

    // === UI -> SETTINGS (User Input) ===

    private void OnQualityDropdownChanged(int value)
    {
        if (!isInitialized) return;
        settings.SetQualityLevel(value);
    }

    private void OnVolumeSliderChanged(float value)
    {
        if (!isInitialized) return;

        // Convert 0-100 to 0-1 for AudioListener
        float normalizedValue = value / 100f;
        settings.SetMasterVolume(normalizedValue);
        UpdateVolumeText(value);
    }

    private void OnResolutionDropdownChanged(int value)
    {
        if (!isInitialized) return;
        settings.SetResolution(value);
    }

    private void OnFullscreenToggleChanged(bool value)
    {
        if (!isInitialized) return;
        settings.SetFullscreen(value);
    }

    // === SETTINGS -> UI (External Changes) ===

    private void OnSettingsQualityChanged(int value)
    {
        if (qualityDropdown != null && qualityDropdown.value != value)
        {
            qualityDropdown.SetValueWithoutNotify(value);
        }
    }

    private void OnSettingsVolumeChanged(float value)
    {
        // Convert 0-1 to 0-100 for slider display
        float sliderValue = value * 100f;

        if (volumeSlider != null && !Mathf.Approximately(volumeSlider.value, sliderValue))
        {
            volumeSlider.SetValueWithoutNotify(sliderValue);
            UpdateVolumeText(sliderValue);
        }
    }

    private void OnSettingsResolutionChanged(Resolution res)
    {
        int index = settings.GetCurrentResolutionIndex();
        if (resolutionDropdown != null && resolutionDropdown.value != index)
        {
            resolutionDropdown.SetValueWithoutNotify(index);
        }
    }

    private void OnSettingsFullscreenChanged(bool value)
    {
        if (fullscreenToggle != null && fullscreenToggle.isOn != value)
        {
            fullscreenToggle.SetIsOnWithoutNotify(value);
        }
    }

    // === BUTTON HANDLERS ===

    private void OnResetClicked()
    {
        settings.ResetToDefaults();
        UpdateUIFromSettings();
        Debug.Log("[SettingsUI] Settings reset to defaults");
    }

    private void OnCloseClicked()
    {
        // Close settings menu (can be overridden or connected to menu system)
        gameObject.SetActive(false);
    }

    // === UPDATE UI FROM SETTINGS ===

    private void UpdateUIFromSettings()
    {
        if (settings == null) return;

        // Quality
        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(settings.GetCurrentQualityLevel());
        }

        // Volume (convert 0-1 to 0-100 for slider)
        if (volumeSlider != null)
        {
            float volume = settings.GetMasterVolume();
            float sliderValue = volume * 100f;
            volumeSlider.SetValueWithoutNotify(sliderValue);
            UpdateVolumeText(sliderValue);
        }

        // Resolution
        if (resolutionDropdown != null)
        {
            resolutionDropdown.SetValueWithoutNotify(settings.GetCurrentResolutionIndex());
        }

        // Fullscreen
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(settings.GetFullscreen());
        }
    }

    // === HELPERS ===

    private void UpdateVolumeText(float sliderValue)
    {
        if (volumeValueText != null)
        {
            // sliderValue is already 0-100, just round and display
            volumeValueText.text = $"{Mathf.RoundToInt(sliderValue)}%";
        }
    }
}