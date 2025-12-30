using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lock timing puzzle: align rotating symbols with center zone.
/// Player must press button when correct symbol is aligned.
/// Uses coroutine for smooth rotation without Update().
/// </summary>
public class LockTimingPuzzle : PuzzleBase
{
    [Header("Config")]
    [SerializeField] private LockTimingPuzzleConfig config;

    [Header("UI Elements")]
    [SerializeField] private RectTransform symbolColumn;
    [SerializeField] private GameObject symbolPrefab;
    [SerializeField] private Button submitButton;
    [SerializeField] private RectTransform centerZone;
    [SerializeField] private TMP_Text progressText;

    [Header("Visual Feedback")]
    [SerializeField] private Image feedbackImage;
    [SerializeField] private float feedbackDuration = 0.3f;

    private List<char> correctSequence = new();
    private List<TMP_Text> symbolTexts = new();
    private int currentIndex;
    private Coroutine rotationCoroutine;
    private bool canInput = true;
    private float currentOffset;

    protected override void Awake()
    {
        base.Awake();

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitPressed);
    }

    private void OnDestroy()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveListener(OnSubmitPressed);
    }

    protected override void OnPuzzleStart()
    {
        GenerateSequence();
        CreateSymbolColumn();
        UpdateProgress();
        StartRotation();

        if (feedbackImage != null)
            feedbackImage.gameObject.SetActive(false);
    }

    protected override void OnPuzzleCancel()
    {
        StopRotation();
        ClearSymbols();
    }

    protected override void OnPuzzleComplete()
    {
        StopRotation();
    }

    protected override void OnPuzzleFail()
    {
        StopRotation();
    }

    // === Sequence Generation ===

    private void GenerateSequence()
    {
        correctSequence.Clear();
        currentIndex = 0;

        char[] pool = config.GetSymbolPool();

        for (int i = 0; i < config.sequenceLength; i++)
        {
            char symbol = pool[Random.Range(0, pool.Length)];
            correctSequence.Add(symbol);
        }

        Debug.Log($"[LockTimingPuzzle] Generated sequence: {string.Join("", correctSequence)}");
    }

    // === Symbol Column Creation ===

    private void CreateSymbolColumn()
    {
        ClearSymbols();

        char[] pool = config.GetSymbolPool();
        int totalSymbols = config.visibleSymbolsInColumn;

        // Create circular buffer of symbols
        for (int i = 0; i < totalSymbols; i++)
        {
            GameObject go = Instantiate(symbolPrefab, symbolColumn);
            TMP_Text text = go.GetComponent<TMP_Text>();

            if (text != null)
            {
                // Fill with random symbols (target will align during rotation)
                char symbol = pool[Random.Range(0, pool.Length)];
                text.text = symbol.ToString();
                text.color = config.neutralColor;
                symbolTexts.Add(text);
            }

            // Position vertically
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -i * config.symbolSpacing);
        }

        // Ensure first correct symbol is in the sequence
        if (symbolTexts.Count > 0)
            symbolTexts[0].text = correctSequence[currentIndex].ToString();
    }

    private void ClearSymbols()
    {
        if (symbolColumn != null)
        {
            for (int i = symbolColumn.childCount - 1; i >= 0; i--)
                Destroy(symbolColumn.GetChild(i).gameObject);
        }

        symbolTexts.Clear();
    }

    // === Rotation Logic ===

    private void StartRotation()
    {
        StopRotation();
        rotationCoroutine = StartCoroutine(RotateSymbolsCoroutine());
    }

    private void StopRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    private IEnumerator RotateSymbolsCoroutine()
    {
        currentOffset = 0f;

        while (true)
        {
            currentOffset += config.rotationSpeed * config.symbolSpacing * Time.deltaTime;

            // Wrap around when exceeding total height
            float totalHeight = config.visibleSymbolsInColumn * config.symbolSpacing;
            if (currentOffset >= config.symbolSpacing)
                currentOffset -= config.symbolSpacing;

            // Update positions
            for (int i = 0; i < symbolTexts.Count; i++)
            {
                RectTransform rt = symbolTexts[i].transform as RectTransform;
                float baseY = -i * config.symbolSpacing;
                float newY = baseY + currentOffset;

                // Wrap symbols that go off screen
                if (newY > config.symbolSpacing)
                    newY -= totalHeight;

                rt.anchoredPosition = new Vector2(0, newY);

                // Highlight symbol near center
                float distanceFromCenter = Mathf.Abs(newY);
                symbolTexts[i].color = distanceFromCenter < config.alignmentTolerance
                    ? config.targetColor
                    : config.neutralColor;
            }

            yield return null;
        }
    }

    // === Input Handling ===

    private void OnSubmitPressed()
    {
        if (!isActive || !canInput) return;

        char targetSymbol = correctSequence[currentIndex];
        bool isCorrect = CheckAlignment(targetSymbol);

        if (isCorrect)
        {
            OnCorrectInput();
        }
        else
        {
            OnIncorrectInput();
        }

        StartCoroutine(InputCooldownCoroutine());
    }

    private bool CheckAlignment(char targetSymbol)
    {
        // Find symbol closest to center (y = 0)
        float closestDistance = float.MaxValue;
        TMP_Text closestSymbol = null;

        foreach (var text in symbolTexts)
        {
            RectTransform rt = text.transform as RectTransform;
            float distance = Mathf.Abs(rt.anchoredPosition.y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSymbol = text;
            }
        }

        // Check if within tolerance and correct symbol
        if (closestDistance <= config.alignmentTolerance &&
            closestSymbol != null &&
            closestSymbol.text[0] == targetSymbol)
        {
            return true;
        }

        return false;
    }

    private void OnCorrectInput()
    {
        Debug.Log($"[LockTimingPuzzle] Correct! {currentIndex + 1}/{config.sequenceLength}");

        ShowFeedback(config.targetColor);
        currentIndex++;

        if (currentIndex >= config.sequenceLength)
        {
            CompletePuzzle();
        }
        else
        {
            UpdateProgress();
            // Update column with next target symbol
            if (symbolTexts.Count > 0)
                symbolTexts[Random.Range(0, symbolTexts.Count)].text = correctSequence[currentIndex].ToString();
        }
    }

    private void OnIncorrectInput()
    {
        Debug.Log("[LockTimingPuzzle] Incorrect alignment!");
        ShowFeedback(config.errorColor);
        FailPuzzle();
    }

    private IEnumerator InputCooldownCoroutine()
    {
        canInput = false;
        yield return new WaitForSeconds(config.inputCooldown);
        canInput = true;
    }

    // === UI Feedback ===

    private void UpdateProgress()
    {
        if (progressText != null)
            progressText.text = $"{currentIndex}/{config.sequenceLength}";
    }

    private void ShowFeedback(Color color)
    {
        if (feedbackImage != null)
            StartCoroutine(FeedbackFlashCoroutine(color));
    }

    private IEnumerator FeedbackFlashCoroutine(Color color)
    {
        feedbackImage.gameObject.SetActive(true);
        feedbackImage.color = color;

        yield return new WaitForSeconds(feedbackDuration);

        feedbackImage.gameObject.SetActive(false);
    }
}