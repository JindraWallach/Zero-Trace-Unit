using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject[] disableDuringHack;

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

        Debug.Log("[UIManager] Entered Hack Mode");
    }

    public void ExitHackMode()
    {
        foreach (var obj in disableDuringHack)
        {
            if (obj != null) obj.SetActive(true);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[UIManager] Exited Hack Mode");
    }
}