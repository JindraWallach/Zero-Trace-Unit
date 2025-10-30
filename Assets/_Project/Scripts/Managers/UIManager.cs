using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // TODO(F2): Centralizovat prompty
    public void ShowPrompt(string text)
    {
        Debug.Log("Show prompt: " + text);
    }

    public void HidePrompt()
    {
        Debug.Log("Hide prompt");
    }

    public void ShowHackOverlay()
    {
        Debug.Log("Show hack overlay");
    }

    public void HideHackOverlay()
    {
        Debug.Log("Hide hack overlay");
    }
}