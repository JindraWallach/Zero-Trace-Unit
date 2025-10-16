using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string text = "Interact";

    private void Awake()
    {
        Hide();
    }

    public void Show(string customText = null)
    {
        gameObject.SetActive(true);
        promptText.text = $"[E] {customText ?? text}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
