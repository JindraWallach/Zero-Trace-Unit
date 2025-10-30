using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main HUD (battery, alerts, indicators).
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField] private Slider batterySlider;
    [SerializeField] private TextMeshProUGUI batteryText;

    [Header("Alerts")]
    [SerializeField] private TextMeshProUGUI alertText;
    [SerializeField] private float alertDuration = 3f;

    private float alertTimer;

    private void Update()
    {
        if (alertTimer > 0f)
        {
            alertTimer -= Time.deltaTime;
            if (alertTimer <= 0f)
                HideAlert();
        }
    }

    public void UpdateBattery(float percent)
    {
        if (batterySlider != null)
            batterySlider.value = percent;

        if (batteryText != null)
            batteryText.text = $"{Mathf.RoundToInt(percent * 100)}%";
    }

    public void ShowAlert(string message)
    {
        if (alertText != null)
        {
            alertText.text = message;
            alertText.gameObject.SetActive(true);
        }

        alertTimer = alertDuration;
    }

    private void HideAlert()
    {
        if (alertText != null)
            alertText.gameObject.SetActive(false);
    }
}