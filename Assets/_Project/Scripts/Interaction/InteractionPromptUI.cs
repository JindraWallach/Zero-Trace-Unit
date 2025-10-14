using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("Interactable Settings")]

    [Tooltip("Root GO")]
    [SerializeField] private GameObject root;
    [Tooltip("Text component for the prompt")]
    [SerializeField] private TextMeshProUGUI promptText;
    [Tooltip("Shown text")]
    [SerializeField] private string text = "Hello";


    public void SetUp()
    {
        root.SetActive(true);
        promptText.text = $"[E] {text}";
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    public void Show()
    {
        root.SetActive(true);
    }
}
