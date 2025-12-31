using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lock timing puzzle: align rotating symbols with center zone.
/// Symbols scroll infinitely in a circular buffer with wraparound.
/// </summary>
public class LockTimingPuzzle : PuzzleBase
{
    [Header("Config")]
    [SerializeField] private LockTimingPuzzleConfig config;

    [Header("UI Elements")]
    [SerializeField] private RectTransform symbolColumn;
    [SerializeField] private RectTransform scrollViewport; // Mask area
    [SerializeField] private GameObject symbolPrefab;
    [SerializeField] private Button submitButton;
    [SerializeField] private RectTransform centerZoneMarker; // Fixed green line
    [SerializeField] private TMP_Text progressText;

    [Header("Visual Feedback")]
    [SerializeField] private Image feedbackImage;
    [SerializeField] private float feedbackDuration = 0.3f;

    private List<char> correctSequence = new();
    private List<SymbolItem> symbolItems = new();
    private int currentTargetIndex;
    private Coroutine rotationCoroutine;
    private bool canInput = true;

    private class SymbolItem
    {
        public GameObject gameObject;
        public TMP_Text text;
        public RectTransform rectTransform;
        public char character;
    }

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
        currentTargetIndex = 0;

        char[] pool = config.GetSymbolPool();

        for (int i = 0; i < config.sequenceLength; i++)
        {
            char symbol = pool[Random.Range(0, pool.Length)];
            correctSequence.Add(symbol);
        }

        Debug.Log($"[LockTimingPuzzle] Sequence: {string.Join("", correctSequence)}");
    }

    // === Symbol Column Creation ===

    private void CreateSymbolColumn()
    {
        ClearSymbols();

        char[] pool = config.GetSymbolPool();
        int totalSymbols = config.visibleSymbolsInColumn;

        // Create fixed circular buffer with random symbols
        List<char> symbolBuffer = new List<char>();

        // Fill with random symbols from pool
        for (int i = 0; i < totalSymbols; i++)
        {
            symbolBuffer.Add(pool[Random.Range(0, pool.Length)]);
        }

        // Ensure ALL correct sequence symbols are present in buffer
        foreach (char target in correctSequence)
        {
            if (!symbolBuffer.Contains(target))
            {
                // Replace random symbol with missing target
                int replaceIndex = Random.Range(0, symbolBuffer.Count);
                symbolBuffer[replaceIndex] = target;
            }
        }

        // Create UI elements (these stay for entire puzzle)
        for (int i = 0; i < totalSymbols; i++)
        {
            GameObject go = Instantiate(symbolPrefab, symbolColumn);
            TMP_Text text = go.GetComponent<TMP_Text>();
            RectTransform rt = go.GetComponent<RectTransform>();

            if (text != null && rt != null)
            {
                SymbolItem item = new SymbolItem
                {
                    gameObject = go,
                    text = text,
                    rectTransform = rt,
                    character = symbolBuffer[i]
                };

                item.text.text = item.character.ToString();
                item.text.color = config.neutralColor;
                symbolItems.Add(item);

                // Initial position (staggered vertically)
                rt.anchoredPosition = new Vector2(0, -i * config.symbolSpacing);
            }
        }

        Debug.Log($"[LockTimingPuzzle] Created {symbolItems.Count} symbols: {string.Join("", symbolBuffer)}");
    }

    private void ClearSymbols()
    {
        if (symbolColumn != null)
        {
            for (int i = symbolColumn.childCount - 1; i >= 0; i--)
                Destroy(symbolColumn.GetChild(i).gameObject);
        }

        symbolItems.Clear();
    }

    // === Rotation Logic (Infinite Wraparound) ===

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
        while (true)
        {
            float moveSpeed = config.rotationSpeed * config.symbolSpacing * Time.deltaTime;
            char currentTarget = correctSequence[currentTargetIndex];

            for (int i = 0; i < symbolItems.Count; i++)
            {
                var item = symbolItems[i];
                Vector2 pos = item.rectTransform.anchoredPosition;

                // Move DOWN (scrolling down visually)
                pos.y -= moveSpeed;

                // Wraparound: if symbol goes below bottom, teleport to top
                float viewportHeight = config.visibleSymbolsInColumn * config.symbolSpacing;
                float bottomThreshold = -viewportHeight;

                if (pos.y < bottomThreshold)
                {
                    // Teleport to top - DON'T change the character!
                    pos.y += viewportHeight;
                }

                item.rectTransform.anchoredPosition = pos;

                // Highlight ONLY the correct target symbol (green)
                bool isTargetSymbol = item.character == currentTarget;
                item.text.color = isTargetSymbol ? config.targetColor : config.neutralColor;
            }

            yield return null;
        }
    }

    private void EnsureCorrectTargetExists()
    {
        char target = correctSequence[currentTargetIndex];
        bool hasTarget = false;

        foreach (var item in symbolItems)
        {
            if (item.character == target)
            {
                hasTarget = true;
                break;
            }
        }

        // If target missing, replace a random symbol
        if (!hasTarget && symbolItems.Count > 0)
        {
            var randomItem = symbolItems[Random.Range(0, symbolItems.Count)];
            randomItem.character = target;
            randomItem.text.text = target.ToString();
        }
    }

    // === Input Handling ===

    private void OnSubmitPressed()
    {
        if (!isActive || !canInput) return;

        char targetSymbol = correctSequence[currentTargetIndex];
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
        // Find symbol closest to center (y ≈ 0)
        float closestDistance = float.MaxValue;
        SymbolItem closestItem = null;

        foreach (var item in symbolItems)
        {
            float distance = Mathf.Abs(item.rectTransform.anchoredPosition.y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        // Check tolerance and correct symbol
        if (closestDistance <= config.alignmentTolerance &&
            closestItem != null &&
            closestItem.character == targetSymbol)
        {
            return true;
        }

        return false;
    }

    private void OnCorrectInput()
    {
        Debug.Log($"[LockTimingPuzzle] ✓ Correct! {currentTargetIndex + 1}/{config.sequenceLength}");

        ShowFeedback(config.targetColor);
        currentTargetIndex++;

        if (currentTargetIndex >= config.sequenceLength)
        {
            CompletePuzzle();
        }
        else
        {
            UpdateProgress();
            // Next target will be ensured by EnsureCorrectTargetExists()
        }
    }

    private void OnIncorrectInput()
    {
        Debug.Log("[LockTimingPuzzle] ✗ Wrong symbol!");
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
            progressText.text = $"{currentTargetIndex}/{config.sequenceLength}";
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