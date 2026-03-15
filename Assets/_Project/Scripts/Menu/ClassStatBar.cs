using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Visual bar for displaying class stats.
/// UPDATED: Works with percentage system (0-200%, default 100%).
/// Maintains backward compatibility with existing UI structure.
/// </summary>
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
        // Start at 50% (which represents 100% in new system)
        if (fillBar != null)
            fillBar.fillAmount = 0.5f;
    }

    /// <summary>
    /// Set stat from PlayerClassConfig (NEW percentage system).
    /// </summary>
    public void SetStat(PlayerClassConfig classConfig)
    {
        if (classConfig == null) return;

        // Get percentage (0-200%)
        int percentage = classConfig.GetStatPercentage(currentStatType);

        // Convert to fill amount (0-200% maps to 0-1)
        float targetFill = percentage / 200f;

        // Update text
        if (statValueText != null)
        {
            if (showPercentage)
                statValueText.text = $"{percentage}%";
            else
                statValueText.text = $"{percentage}/200";
        }

        // Update color (gradient evaluates 0-1, where 0.5 = 100% = normal)
        if (colorCodeBar && fillBar != null && barGradient != null)
            fillBar.color = barGradient.Evaluate(targetFill);

        // Animate or set immediately
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

    /// <summary>
    /// DEPRECATED: Legacy method for old 0-10 system.
    /// Kept for backward compatibility but converts to new system.
    /// </summary>
    [System.Obsolete("Use SetStat(PlayerClassConfig) instead. This uses old 0-10 scale.")]
    public void SetStatValue(int value, string percentage = "")
    {
        // Convert old 0-10 scale to new 0-200% scale
        // value 5 (middle) = 100%
        // value 0 = 0%, value 10 = 200%
        int convertedPercentage = value * 20;

        float targetFill = convertedPercentage / 200f;

        if (statValueText != null)
        {
            if (showPercentage && !string.IsNullOrEmpty(percentage))
                statValueText.text = percentage;
            else if (showPercentage)
                statValueText.text = $"{convertedPercentage}%";
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