using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ClassStatBar : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statNameText;
    [SerializeField] private TextMeshProUGUI statValueText;
    [SerializeField] private Image fillBar;
    [SerializeField] private Image iconImage;

    [Header("Visual Settings")]
    [SerializeField] private bool showPercentage = true;
    [SerializeField] private bool colorCodeBar = true;
    [SerializeField] private Gradient barGradient;

    [Header("Animation")]
    [SerializeField] private bool animateFill = true;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Stat Icons")]
    [SerializeField] private Sprite speedIcon;
    [SerializeField] private Sprite stealthIcon;
    [SerializeField] private Sprite hackingIcon;
    [SerializeField] private Sprite detectionIcon;

    private StatType currentStatType;
    private Coroutine animationCoroutine;

    public void Initialize(StatType type)
    {
        currentStatType = type;
        if (statNameText != null)
            statNameText.text = GetStatDisplayName(type);
        if (iconImage != null)
        {
            Sprite icon = GetStatIcon(type);
            if (icon != null)
                iconImage.sprite = icon;
        }
        if (fillBar != null)
            fillBar.fillAmount = 0.5f;
    }

    public void SetStat(PlayerClassConfig classConfig)
    {
        if (classConfig == null) return;
        int normalizedValue = classConfig.GetNormalizedStat(currentStatType);
        string percentage = classConfig.GetStatPercentage(currentStatType);
        SetStatValue(normalizedValue, percentage);
    }

    public void SetStatValue(int value, string percentage = "")
    {
        value = Mathf.Clamp(value, 0, 10);
        float targetFill = value / 10f;

        if (statValueText != null)
        {
            if (showPercentage && !string.IsNullOrEmpty(percentage))
                statValueText.text = percentage;
            else
                statValueText.text = $"{value}/10";
        }

        if (colorCodeBar && fillBar != null && barGradient != null)
            fillBar.color = barGradient.Evaluate(targetFill);

        if (animateFill && fillBar != null)
        {
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateFillCoroutine(targetFill));
        }
        else if (fillBar != null)
        {
            fillBar.fillAmount = targetFill;
        }
    }

    private IEnumerator AnimateFillCoroutine(float targetFill)
    {
        float startFill = fillBar.fillAmount;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            fillBar.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            yield return null;
        }

        fillBar.fillAmount = targetFill;
        animationCoroutine = null;
    }

    private string GetStatDisplayName(StatType type)
    {
        return type switch
        {
            StatType.Speed => "Speed",
            StatType.Stealth => "Stealth",
            StatType.Hacking => "Hacking",
            StatType.Detection => "Detection",
            _ => "Unknown"
        };
    }

    private Sprite GetStatIcon(StatType type)
    {
        return type switch
        {
            StatType.Speed => speedIcon,
            StatType.Stealth => stealthIcon,
            StatType.Hacking => hackingIcon,
            StatType.Detection => detectionIcon,
            _ => null
        };
    }

    private void OnDestroy()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
    }
}