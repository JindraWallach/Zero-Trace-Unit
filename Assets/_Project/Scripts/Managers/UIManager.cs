using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central UI controller (singleton).
/// - Manages all prompts (world-space + HUD)
/// - Shows/hides hack overlays
/// - Handles cursor state
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private HUDController hudController;
    [SerializeField] private HackOverlayUI hackOverlay;
    [SerializeField] private GameObject[] disableDuringHack; // minimap, etc.

    private readonly List<UIPromptController> activePrompts = new();
    private bool isInHackMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // === Prompt Management ===
    public void RegisterPrompt(UIPromptController prompt)
    {
        if (prompt != null && !activePrompts.Contains(prompt))
            activePrompts.Add(prompt);
    }

    public void UnregisterPrompt(UIPromptController prompt)
    {
        if (prompt != null)
            activePrompts.Remove(prompt);
    }

    public void HideAllPrompts()
    {
        foreach (var p in activePrompts)
            p?.Hide();
    }

    // === Hack Mode ===
    public void EnterHackMode()
    {
        isInHackMode = true;

        // Hide world prompts
        HideAllPrompts();

        // Disable UI elements
        foreach (var obj in disableDuringHack)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Show hack overlay
        hackOverlay?.Show();

        // Unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[UIManager] Entered Hack Mode");
    }

    public void ExitHackMode()
    {
        isInHackMode = false;

        // Re-enable UI
        foreach (var obj in disableDuringHack)
        {
            if (obj != null) obj.SetActive(true);
        }

        // Hide overlay
        hackOverlay?.Hide();

        // Lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[UIManager] Exited Hack Mode");
    }

    // === HUD Access ===
    public void UpdateBattery(float percent)
    {
        hudController?.UpdateBattery(percent);
    }

    public void ShowAlert(string message)
    {
        hudController?.ShowAlert(message);
    }
}