using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ClassSelectionUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private PlayerClassSelector classSelector;
    [SerializeField] private GameObject menuPlayer; // Player in menu scene

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
    [SerializeField] private Image classPreviewImage;

    [Header("Animation")]
    [SerializeField] private bool animateStatBars = true;
    [SerializeField] private float statBarAnimationDelay = 0.1f;

    private ClassStatBar[] generatedStatBars;
    private PlayerClassApplier playerApplier;

    private void Start()
    {
        if (classSelector == null)
        {
            Debug.LogError("[ClassSelectionUI] PlayerClassSelector not assigned!");
            enabled = false;
            return;
        }

        // Find player applier
        if (menuPlayer != null)
        {
            playerApplier = menuPlayer.GetComponent<PlayerClassApplier>();
            if (playerApplier == null)
                Debug.LogWarning("[ClassSelectionUI] Menu player missing PlayerClassApplier!");
        }

        BindButtons();
        GenerateStatBars();
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousClicked);
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);
    }

    private void UnbindButtons()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(OnPreviousClicked);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextClicked);
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    private void GenerateStatBars()
    {
        if (statsContainer == null || statBarPrefab == null) return;

        foreach (Transform child in statsContainer)
            Destroy(child.gameObject);

        StatType[] statTypes = (StatType[])System.Enum.GetValues(typeof(StatType));
        generatedStatBars = new ClassStatBar[statTypes.Length];

        for (int i = 0; i < statTypes.Length; i++)
        {
            GameObject barObj = Instantiate(statBarPrefab, statsContainer);
            ClassStatBar bar = barObj.GetComponent<ClassStatBar>();
            if (bar != null)
            {
                bar.Initialize(statTypes[i]);
                generatedStatBars[i] = bar;
            }
        }
    }

    private void OnPreviousClicked()
    {
        classSelector.SelectPreviousClass();
        UpdateUI();
    }

    private void OnNextClicked()
    {
        classSelector.SelectNextClass();
        UpdateUI();
    }

    private void OnSelectClicked()
    {
        classSelector.ConfirmSelection();
        ApplyClassToMenuPlayer();
    }

    private void ApplyClassToMenuPlayer()
    {
        if (playerApplier == null || classSelector.CurrentClass == null) return;

        playerApplier.ApplyClass(classSelector.CurrentClass);
        Debug.Log($"[ClassSelectionUI] Applied {classSelector.CurrentClass.className} to menu player");
    }

    private void UpdateUI()
    {
        PlayerClassConfig currentClass = classSelector.CurrentClass;
        if (currentClass == null) return;

        UpdateTexts(currentClass);
        UpdateStatBars(currentClass);
        UpdateVisuals(currentClass);
        UpdatePreviewSprite(currentClass);
    }

    private void UpdateTexts(PlayerClassConfig classConfig)
    {
        if (classNameText != null)
            classNameText.text = classConfig.className.ToUpper();
        if (classPrefixText != null)
            classPrefixText.text = classConfig.classPrefix.ToUpper();
        if (descriptionText != null)
            descriptionText.text = classConfig.description;
    }

    private void UpdateStatBars(PlayerClassConfig classConfig)
    {
        if (generatedStatBars == null || generatedStatBars.Length == 0) return;

        if (animateStatBars)
            StartCoroutine(AnimateStatBarsSequentially(classConfig));
        else
        {
            foreach (var bar in generatedStatBars)
            {
                if (bar != null)
                    bar.SetStat(classConfig);
            }
        }
    }

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

    private void UpdateVisuals(PlayerClassConfig classConfig)
    {
        if (classIcon != null && classConfig.classIcon != null)
            classIcon.sprite = classConfig.classIcon;

        if (backgroundTint != null)
        {
            Color tintColor = classConfig.primaryColor;
            tintColor.a = 0.3f;
            backgroundTint.color = tintColor;
        }
    }

    private void UpdatePreviewSprite(PlayerClassConfig classConfig)
    {
        if (classPreviewImage == null) return;

        if (classConfig.classPreviewSprite != null)
        {
            classPreviewImage.sprite = classConfig.classPreviewSprite;
            classPreviewImage.color = Color.white;
        }
        else if (classConfig.classIcon != null)
        {
            classPreviewImage.sprite = classConfig.classIcon;
            classPreviewImage.color = classConfig.primaryColor;
        }
        else
        {
            classPreviewImage.sprite = null;
            classPreviewImage.color = classConfig.primaryColor;
        }
    }
}