using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Universal UI controller for class selection carousel.
/// Dynamically generates stat bars based on PlayerClassConfig.
/// SRP: UI presentation only, no update loops.
/// </summary>
public class ClassSelectionUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private PlayerClassSelector classSelector;

    [Header("Text Elements")]
    [SerializeField] private TextMeshProUGUI classNameText;
    [SerializeField] private TextMeshProUGUI classPrefixText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Stat Bar Generation")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statBarPrefab;

    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button selectButton;

    [Header("Visual Elements")]
    [SerializeField] private Image classIcon;
    [SerializeField] private Image backgroundTint;

    [Header("3D Model Preview (Optional)")]
    [SerializeField] private Transform modelPreviewParent;
    [SerializeField] private bool enableModelPreview = true;
    [SerializeField] private float modelRotationSpeed = 30f;

    [Header("Animation Settings")]
    [SerializeField] private bool animateStatBars = true;
    [SerializeField] private float statBarAnimationDelay = 0.1f;

    private GameObject currentModelPreview;
    private ClassStatBar[] generatedStatBars;
    private Coroutine rotationCoroutine;

    private void Start()
    {
        if (classSelector == null)
        {
            Debug.LogError("[ClassSelectionUI] PlayerClassSelector not assigned!");
            enabled = false;
            return;
        }

        BindButtons();
        GenerateStatBars();
        UpdateUI();
    }

    private void OnDestroy()
    {
        UnbindButtons();
        StopModelRotation();
    }

    /// <summary>
    /// Bind button events.
    /// </summary>
    private void BindButtons()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);
    }

    /// <summary>
    /// Unbind button events.
    /// </summary>
    private void UnbindButtons()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(OnPreviousClicked);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);

        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    /// <summary>
    /// Generate stat bars dynamically from enum.
    /// Called once on Start.
    /// </summary>
    private void GenerateStatBars()
    {
        if (statsContainer == null)
        {
            Debug.LogWarning("[ClassSelectionUI] Stats container not assigned!");
            return;
        }

        if (statBarPrefab == null)
        {
            Debug.LogError("[ClassSelectionUI] Stat bar prefab not assigned!");
            return;
        }

        // Clear existing bars
        foreach (Transform child in statsContainer)
        {
            Destroy(child.gameObject);
        }

        // Get all stat types
        StatType[] statTypes = (StatType[])System.Enum.GetValues(typeof(StatType));
        generatedStatBars = new ClassStatBar[statTypes.Length];

        // Create bar for each stat type
        for (int i = 0; i < statTypes.Length; i++)
        {
            GameObject barObj = Instantiate(statBarPrefab, statsContainer);
            ClassStatBar bar = barObj.GetComponent<ClassStatBar>();

            if (bar != null)
            {
                bar.Initialize(statTypes[i]);
                generatedStatBars[i] = bar;
            }
            else
            {
                Debug.LogError("[ClassSelectionUI] Stat bar prefab missing ClassStatBar component!");
            }
        }
    }

    /// <summary>
    /// Handle previous button click.
    /// </summary>
    private void OnPreviousClicked()
    {
        classSelector.SelectPreviousClass();
        UpdateUI();
    }

    /// <summary>
    /// Handle next button click.
    /// </summary>
    private void OnNextClicked()
    {
        classSelector.SelectNextClass();
        UpdateUI();
    }

    /// <summary>
    /// Handle select button click.
    /// </summary>
    private void OnSelectClicked()
    {
        classSelector.ConfirmSelection();
    }

    /// <summary>
    /// Update entire UI to reflect current class.
    /// No Update() loop - called only on class change.
    /// </summary>
    private void UpdateUI()
    {
        PlayerClassConfig currentClass = classSelector.CurrentClass;

        if (currentClass == null)
        {
            Debug.LogWarning("[ClassSelectionUI] No class selected!");
            return;
        }

        UpdateTexts(currentClass);
        UpdateStatBars(currentClass);
        UpdateVisuals(currentClass);

        if (enableModelPreview)
            UpdateModelPreview(currentClass);
    }

    /// <summary>
    /// Update text elements.
    /// </summary>
    private void UpdateTexts(PlayerClassConfig classConfig)
    {
        if (classNameText != null)
            classNameText.text = classConfig.className.ToUpper();

        if (classPrefixText != null)
            classPrefixText.text = classConfig.classPrefix.ToUpper();

        if (descriptionText != null)
            descriptionText.text = classConfig.description;
    }

    /// <summary>
    /// Update all stat bars.
    /// </summary>
    private void UpdateStatBars(PlayerClassConfig classConfig)
    {
        if (generatedStatBars == null || generatedStatBars.Length == 0)
            return;

        if (animateStatBars)
        {
            StartCoroutine(AnimateStatBarsSequentially(classConfig));
        }
        else
        {
            foreach (var bar in generatedStatBars)
            {
                if (bar != null)
                    bar.SetStat(classConfig);
            }
        }
    }

    /// <summary>
    /// Animate stat bars one by one (optional polish).
    /// </summary>
    private IEnumerator AnimateStatBarsSequentially(PlayerClassConfig classConfig)
    {
        foreach (var bar in generatedStatBars)
        {
            if (bar != null)
            {
                bar.SetStat(classConfig);
                yield return new WaitForSeconds(statBarAnimationDelay);
            }
        }
    }

    /// <summary>
    /// Update visual elements (icon, colors).
    /// </summary>
    private void UpdateVisuals(PlayerClassConfig classConfig)
    {
        if (classIcon != null && classConfig.classIcon != null)
            classIcon.sprite = classConfig.classIcon;

        if (backgroundTint != null)
        {
            Color tintColor = classConfig.primaryColor;
            tintColor.a = 0.3f; // Semi-transparent
            backgroundTint.color = tintColor;
        }
    }

    /// <summary>
    /// Update 3D model preview.
    /// </summary>
    private void UpdateModelPreview(PlayerClassConfig classConfig)
    {
        if (modelPreviewParent == null)
            return;

        // Stop old rotation
        StopModelRotation();

        // Destroy old preview
        if (currentModelPreview != null)
        {
            Destroy(currentModelPreview);
        }

        // Spawn new preview
        if (classConfig.playerPrefab != null)
        {
            currentModelPreview = Instantiate(
                classConfig.playerPrefab,
                modelPreviewParent.position,
                modelPreviewParent.rotation,
                modelPreviewParent
            );

            // Disable gameplay components
            DisablePreviewComponents(currentModelPreview);

            // Start rotation
            rotationCoroutine = StartCoroutine(RotateModelCoroutine());
        }
    }

    /// <summary>
    /// Rotate model preview continuously.
    /// Uses coroutine instead of Update().
    /// </summary>
    private IEnumerator RotateModelCoroutine()
    {
        while (currentModelPreview != null)
        {
            currentModelPreview.transform.Rotate(Vector3.up, modelRotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Stop model rotation coroutine.
    /// </summary>
    private void StopModelRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    /// <summary>
    /// Disable gameplay components on preview model.
    /// </summary>
    private void DisablePreviewComponents(GameObject preview)
    {
        // Disable character controller
        var controller = preview.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        // Disable player scripts
        var playerScripts = preview.GetComponents<MonoBehaviour>();
        foreach (var script in playerScripts)
        {
            // Keep only MenuAnimationController active
            if (script.GetType().Name != "MenuAnimationController")
            {
                script.enabled = false;
            }
        }

        // Disable colliders except trigger for visual bounds
        var colliders = preview.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            if (!col.isTrigger)
                col.enabled = false;
        }
    }
}