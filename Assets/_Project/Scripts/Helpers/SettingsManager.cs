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
    [SerializeField] private float defaultMasterVolume = 1f;
    [SerializeField] private bool defaultFullscreen = true;
    [SerializeField] private int defaultTargetFramerate = 60;
    [SerializeField] private bool defaultVSync = true;

    [Header("Debug - Current Values")]
    [SerializeField] private float currentMasterVolume;
    [SerializeField] private int currentResolutionIndex;
    [SerializeField] private bool currentFullscreen;
    [SerializeField] private int currentTargetFramerate;
    [SerializeField] private bool currentVSync;

    // Events for UI updates
    public event Action<float> OnVolumeChanged;
    public event Action<Resolution> OnResolutionChanged;
    public event Action<bool> OnFullscreenChanged;
    public event Action<int> OnTargetFramerateChanged;
    public event Action<bool> OnVSyncChanged;

    // Cached resolutions
    private Resolution[] availableResolutions;
    private Resolution currentResolution;

    // PlayerPrefs keys (const for performance)
    private const string KEY_VOLUME = "MasterVolume";
    private const string KEY_RES_WIDTH = "ResolutionWidth";
    private const string KEY_RES_HEIGHT = "ResolutionHeight";
    private const string KEY_FULLSCREEN = "Fullscreen";
    private const string KEY_TARGET_FPS = "TargetFramerate";
    private const string KEY_VSYNC = "VSync";

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

        // Cache available resolutions (filtered for uniqueness)
        CacheResolutions();

        // Load and apply saved settings
        LoadSettings();
    }

    // === TARGET FRAMERATE SETTINGS ===

    /// <summary>
    /// Get current target framerate (-1 = unlimited).
    /// </summary>
    public int GetTargetFramerate()
    {
        return currentTargetFramerate;
    }

    /// <summary>
    /// Set target framerate (-1 = unlimited, 0 = platform default, >0 = specific FPS).
    /// Common values: 30, 60, 120, 144, 240, -1 (unlimited)
    /// </summary>
    public void SetTargetFramerate(int fps)
    {
        currentTargetFramerate = fps;
        Application.targetFrameRate = fps;
        PlayerPrefs.SetInt(KEY_TARGET_FPS, fps);
        PlayerPrefs.Save();

        OnTargetFramerateChanged?.Invoke(fps);

        string fpsText = fps == -1 ? "Unlimited" : fps.ToString();
        Debug.Log($"[SettingsManager] Target framerate set to: {fpsText}");
    }

    // === VSYNC SETTINGS ===

    /// <summary>
    /// Get current VSync state.
    /// </summary>
    public bool GetVSync()
    {
        return currentVSync;
    }

    /// <summary>
    /// Set VSync on/off.
    /// Note: VSync forces framerate to monitor refresh rate.
    /// </summary>
    public void SetVSync(bool enabled)
    {
        currentVSync = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(KEY_VSYNC, enabled ? 1 : 0);
        PlayerPrefs.Save();

        OnVSyncChanged?.Invoke(enabled);

        Debug.Log($"[SettingsManager] VSync: {(enabled ? "ON" : "OFF")}");
    }

    /// <summary>
    /// Toggle VSync (convenience method).
    /// </summary>
    public void ToggleVSync()
    {
        SetVSync(!currentVSync);
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

        Debug.Log($"[SettingsManager] Volume set to: {volume:F2}");
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
        SetMasterVolume(defaultMasterVolume);
        SetFullscreen(defaultFullscreen);
        SetTargetFramerate(defaultTargetFramerate);
        SetVSync(defaultVSync);

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
        // Load volume
        currentMasterVolume = PlayerPrefs.GetFloat(KEY_VOLUME, defaultMasterVolume);
        currentMasterVolume = Mathf.Clamp01(currentMasterVolume);
        AudioListener.volume = currentMasterVolume;

        // Load target framerate
        currentTargetFramerate = PlayerPrefs.GetInt(KEY_TARGET_FPS, defaultTargetFramerate);
        Application.targetFrameRate = currentTargetFramerate;

        // Load VSync
        currentVSync = PlayerPrefs.GetInt(KEY_VSYNC, defaultVSync ? 1 : 0) == 1;
        QualitySettings.vSyncCount = currentVSync ? 1 : 0;

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
                  $"  Volume: {currentMasterVolume:F2}\n" +
                  $"  Target FPS: {(currentTargetFramerate == -1 ? "Unlimited" : currentTargetFramerate.ToString())}\n" +
                  $"  VSync: {(currentVSync ? "ON" : "OFF")}\n" +
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
        AudioListener.volume = currentMasterVolume;
        Application.targetFrameRate = currentTargetFramerate;
        QualitySettings.vSyncCount = currentVSync ? 1 : 0;
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