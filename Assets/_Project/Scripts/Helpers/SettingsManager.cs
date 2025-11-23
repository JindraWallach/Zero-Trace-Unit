using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Centralized settings management (singleton).
/// Handles quality, volume, resolution, and fullscreen.
/// Persists via PlayerPrefs, applies on startup.
/// SRP: Each setting type has dedicated methods.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Default Settings")]
    [SerializeField] private int defaultQualityLevel = 2; // Medium
    [SerializeField] private float defaultMasterVolume = 1f;
    [SerializeField] private bool defaultFullscreen = true;

    [Header("Debug - Current Values")]
    [SerializeField] private int currentQualityLevel;
    [SerializeField] private float currentMasterVolume;
    [SerializeField] private int currentResolutionIndex;
    [SerializeField] private bool currentFullscreen;

    // Events for UI updates
    public event Action<int> OnQualityChanged;
    public event Action<float> OnVolumeChanged;
    public event Action<Resolution> OnResolutionChanged;
    public event Action<bool> OnFullscreenChanged;

    // Cached resolutions
    private Resolution[] availableResolutions;
    private Resolution currentResolution;

    // PlayerPrefs keys (const for performance)
    private const string KEY_QUALITY = "QualityLevel";
    private const string KEY_VOLUME = "MasterVolume";
    private const string KEY_RES_WIDTH = "ResolutionWidth";
    private const string KEY_RES_HEIGHT = "ResolutionHeight";
    private const string KEY_FULLSCREEN = "Fullscreen";

    private void Awake()
    {
        // Simple singleton - recreated per scene for clean state
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Cache pouze v hlavní instanci
        CacheResolutions();

        // Načtení uložených nastavení
        LoadSettings();
    }

    // === QUALITY SETTINGS ===

    /// <summary>
    /// Get available quality level names.
    /// </summary>
    public string[] GetQualityLevels()
    {
        return QualitySettings.names;
    }

    /// <summary>
    /// Get current quality level index.
    /// </summary>
    public int GetCurrentQualityLevel()
    {
        return currentQualityLevel;
    }

    /// <summary>
    /// Set quality level by index.
    /// </summary>
    public void SetQualityLevel(int index)
    {
        if (index < 0 || index >= QualitySettings.names.Length)
        {
            Debug.LogWarning($"[SettingsManager] Invalid quality level: {index}");
            return;
        }

        currentQualityLevel = index;
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt(KEY_QUALITY, index);
        PlayerPrefs.Save();

        OnQualityChanged?.Invoke(index);

        Debug.Log($"[SettingsManager] Quality set to: {QualitySettings.names[index]}");
    }

    // === VOLUME SETTINGS ===

    /// <summary>
    /// Get current master volume (0-1).
    /// </summary>
    public float GetMasterVolume()
    {
        return currentMasterVolume;
    }

    /// <summary>
    /// Set master volume (0-1).
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        currentMasterVolume = volume;
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(KEY_VOLUME, volume);
        PlayerPrefs.Save();

        OnVolumeChanged?.Invoke(volume);

        //Debug.Log($"[SettingsManager] Volume set to: {volume:F2}");
    }

    // === RESOLUTION SETTINGS ===

    /// <summary>
    /// Get available screen resolutions.
    /// </summary>
    public Resolution[] GetAvailableResolutions()
    {
        return availableResolutions;
    }

    /// <summary>
    /// Get current resolution index in available resolutions array.
    /// </summary>
    public int GetCurrentResolutionIndex()
    {
        return currentResolutionIndex;
    }

    /// <summary>
    /// Get current resolution.
    /// </summary>
    public Resolution GetCurrentResolution()
    {
        return currentResolution;
    }

    /// <summary>
    /// Set resolution by index in available resolutions array.
    /// </summary>
    public void SetResolution(int index)
    {
        if (index < 0 || index >= availableResolutions.Length)
        {
            Debug.LogWarning($"[SettingsManager] Invalid resolution index: {index}");
            return;
        }

        Resolution res = availableResolutions[index];
        SetResolution(res.width, res.height, currentFullscreen);
        currentResolutionIndex = index;
    }

    /// <summary>
    /// Set resolution by width/height.
    /// </summary>
    public void SetResolution(int width, int height, bool fullscreen)
    {
        Screen.SetResolution(width, height, fullscreen);

        currentResolution = new Resolution { width = width, height = height };
        currentFullscreen = fullscreen;

        PlayerPrefs.SetInt(KEY_RES_WIDTH, width);
        PlayerPrefs.SetInt(KEY_RES_HEIGHT, height);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        OnResolutionChanged?.Invoke(currentResolution);

        Debug.Log($"[SettingsManager] Resolution set to: {width}x{height} (Fullscreen: {fullscreen})");
    }

    // === FULLSCREEN SETTINGS ===

    /// <summary>
    /// Get current fullscreen state.
    /// </summary>
    public bool GetFullscreen()
    {
        return currentFullscreen;
    }

    /// <summary>
    /// Toggle fullscreen mode.
    /// </summary>
    public void SetFullscreen(bool fullscreen)
    {
        currentFullscreen = fullscreen;
        Screen.fullScreen = fullscreen;

        PlayerPrefs.SetInt(KEY_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();

        OnFullscreenChanged?.Invoke(fullscreen);

        Debug.Log($"[SettingsManager] Fullscreen: {fullscreen}");
    }

    /// <summary>
    /// Toggle fullscreen (convenience method).
    /// </summary>
    public void ToggleFullscreen()
    {
        SetFullscreen(!currentFullscreen);
    }

    // === RESET SETTINGS ===

    /// <summary>
    /// Reset all settings to defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        SetQualityLevel(defaultQualityLevel);
        SetMasterVolume(defaultMasterVolume);
        SetFullscreen(defaultFullscreen);

        // Set resolution to native
        Resolution nativeRes = Screen.resolutions[Screen.resolutions.Length - 1];
        SetResolution(nativeRes.width, nativeRes.height, defaultFullscreen);

        Debug.Log("[SettingsManager] Settings reset to defaults");
    }

    // === INTERNAL METHODS ===

    private void CacheResolutions()
    {
        // Get all resolutions and filter duplicates (same width/height, different refresh rates)
        var uniqueResolutions = new List<Resolution>();
        var resSet = new HashSet<string>();

        foreach (var res in Screen.resolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (!resSet.Contains(key))
            {
                resSet.Add(key);
                uniqueResolutions.Add(res);
            }
        }

        availableResolutions = uniqueResolutions.ToArray();

        // Sort by resolution size (ascending)
        System.Array.Sort(availableResolutions, (a, b) =>
        {
            int areaA = a.width * a.height;
            int areaB = b.width * b.height;
            return areaA.CompareTo(areaB);
        });

        Debug.Log($"[SettingsManager] Cached {availableResolutions.Length} unique resolutions");
    }

    private void LoadSettings()
    {
        // Load quality
        currentQualityLevel = PlayerPrefs.GetInt(KEY_QUALITY, defaultQualityLevel);
        currentQualityLevel = Mathf.Clamp(currentQualityLevel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(currentQualityLevel);

        // Load volume
        currentMasterVolume = PlayerPrefs.GetFloat(KEY_VOLUME, defaultMasterVolume);
        currentMasterVolume = Mathf.Clamp01(currentMasterVolume);
        AudioListener.volume = currentMasterVolume;

        // Load fullscreen
        currentFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, defaultFullscreen ? 1 : 0) == 1;

        // Load resolution
        if (PlayerPrefs.HasKey(KEY_RES_WIDTH) && PlayerPrefs.HasKey(KEY_RES_HEIGHT))
        {
            int width = PlayerPrefs.GetInt(KEY_RES_WIDTH);
            int height = PlayerPrefs.GetInt(KEY_RES_HEIGHT);

            // Find matching resolution index
            currentResolutionIndex = FindResolutionIndex(width, height);
            currentResolution = new Resolution { width = width, height = height };

            Screen.SetResolution(width, height, currentFullscreen);
        }
        else
        {
            // First launch - use current screen resolution
            currentResolution = Screen.currentResolution;
            currentResolutionIndex = FindResolutionIndex(currentResolution.width, currentResolution.height);
            Screen.fullScreen = currentFullscreen;
        }

        Debug.Log($"[SettingsManager] Settings loaded:\n" +
                  $"  Quality: {QualitySettings.names[currentQualityLevel]}\n" +
                  $"  Volume: {currentMasterVolume:F2}\n" +
                  $"  Resolution: {currentResolution.width}x{currentResolution.height}\n" +
                  $"  Fullscreen: {currentFullscreen}");
    }

    private int FindResolutionIndex(int width, int height)
    {
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == width && availableResolutions[i].height == height)
                return i;
        }

        // If not found, return highest resolution index
        return availableResolutions.Length - 1;
    }

    // === PUBLIC UTILITY METHODS ===

    /// <summary>
    /// Apply settings immediately (useful after loading game).
    /// </summary>
    public void ApplySettings()
    {
        QualitySettings.SetQualityLevel(currentQualityLevel);
        AudioListener.volume = currentMasterVolume;
        Screen.SetResolution(currentResolution.width, currentResolution.height, currentFullscreen);
    }

    /// <summary>
    /// Get formatted resolution string for UI.
    /// </summary>
    public string GetResolutionString(Resolution res)
    {
        return $"{res.width} x {res.height}";
    }

    /// <summary>
    /// Get formatted resolution string for current resolution.
    /// </summary>
    public string GetCurrentResolutionString()
    {
        return GetResolutionString(currentResolution);
    }
}