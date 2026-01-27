using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Universal stat bar component for class selection UI.
/// Automatically configures based on StatType.
/// SRP: Displays single stat with visual feedback.
/// NO Update() - uses coroutine for animation.
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

    [Header("Animation Settings")]
    [SerializeField] private bool animateFill = true;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Stat Icons (Assign in Inspector)")]
    [SerializeField] private Sprite speedIcon;
    [SerializeField] private Sprite stealthIcon;
    [SerializeField] private Sprite hackingIcon;
    [SerializeField] private Sprite detectionIcon;

    private StatType currentStatType;
    private Coroutine animationCoroutine;

    /// <summary>
    /// Initialize bar with specific stat type.
    /// Called by ClassSelectionUI when generating bars.
    /// </summary>
    public void Initialize(StatType type)
    {
        currentStatType = type;

        // Set stat name
        if (statNameText != null)
            statNameText.text = GetStatDisplayName(type);

        // Set icon
        if (iconImage != null)
        {
            Sprite icon = GetStatIcon(type);
            if (icon != null)
                iconImage.sprite = icon;
        }

        // Initialize fill to middle
        if (fillBar != null)
            fillBar.fillAmount = 0.5f;
    }

    /// <summary>
    /// Set stat value from PlayerClassConfig.
    /// </summary>
    public void SetStat(PlayerClassConfig classConfig)
    {
        if (classConfig == null)
            return;

        int normalizedValue = classConfig.GetNormalizedStat(currentStatType);
        string percentage = classConfig.GetStatPercentage(currentStatType);

        SetStatValue(normalizedValue, percentage);
    }

    /// <summary>
    /// Set stat value directly (0-10 scale).
    /// </summary>
    public void SetStatValue(int value, string percentage = "")
    {
        value = Mathf.Clamp(value, 0, 10);
        float targetFill = value / 10f;

        // Update value text
        if (statValueText != null)
        {
            if (showPercentage && !string.IsNullOrEmpty(percentage))
            {
                statValueText.text = percentage;
            }
            else
            {
                statValueText.text = $"{value}/10";
            }
        }

        // Update bar color
        if (colorCodeBar && fillBar != null && barGradient != null)
        {
            fillBar.color = barGradient.Evaluate(targetFill);
        }

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
    /// Animate fill bar to target value.
    /// Uses coroutine instead of Update().
    /// </summary>
    private IEnumerator AnimateFillCoroutine(float targetFill)
    {
        float startFill = fillBar.fillAmount;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            // Smooth lerp
            fillBar.fillAmount = Mathf.Lerp(startFill, targetFill, t);

            yield return null;
        }

        fillBar.fillAmount = targetFill;
        animationCoroutine = null;
    }

    /// <summary>
    /// Get display name for stat type.
    /// </summary>
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

    /// <summary>
    /// Get icon for stat type.
    /// </summary>
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
        {
            StopCoroutine(animationCoroutine);
        }
    }
}