using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject[] disableDuringHack; 
    
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;

    private readonly List<UIPromptController> activePrompts = new();

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

    private void Start()
    {
        HidePauseMenu();
    }

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Debug.Log("[UIManager] Pause menu shown");
        }
        else
        {
            Debug.LogWarning("[UIManager] Pause menu panel not assigned!");
        }
    }

    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("[UIManager] Pause menu hidden");
        }
    }

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

    public void EnterHackMode()
    {
        HideAllPrompts();

        foreach (var obj in disableDuringHack)
        {
            if (obj != null) obj.SetActive(false);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ExitHackMode()
    {
        foreach (var obj in disableDuringHack)
        {
            if (obj != null) obj.SetActive(true);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}