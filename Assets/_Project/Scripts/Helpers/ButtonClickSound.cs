using UnityEngine;
using UnityEngine.UI;
using ZeroTrace.Audio;

public class ButtonClickSound : MonoBehaviour
{
    private string id = "click";
    private void Awake()
    {
        GetComponent<Button>()?.onClick.AddListener(() =>
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("AudioManager instance is null!", this);
                return;
            }
            AudioManager.Instance.Play(id, transform.position);
        });
    }
}
