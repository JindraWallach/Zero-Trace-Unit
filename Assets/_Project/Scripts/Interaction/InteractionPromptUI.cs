using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI promptText;  
    [SerializeField] private CanvasGroup canvasGroup; 

    [Header("Settings")]
    [SerializeField] private string defaultText = "Interact";

    private void Awake()
    {
        Hide();
    }

    public void Show(string customText = null)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        promptText.text = $"[E] {customText ?? defaultText}";
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
