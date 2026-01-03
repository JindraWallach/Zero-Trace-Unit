using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lock timing puzzle: align rotating symbols with center zone.
/// Supports multiple columns (only the active column rotates / accepts input).
/// Symbols scroll infinitely in a circular buffer with wraparound.
/// Uses a single shared center zone marker for all columns.
/// </summary>
public class LockTimingPuzzle : PuzzleBase
{
    [Header("Config")]
    [SerializeField] private LockTimingPuzzleConfig config;

    [Header("UI Elements")]
    [Tooltip("If you provide multiple columns, each column will be created and only the active one will rotate.")]
    [Header("Multi-Column Setup")]
    [SerializeField] private List<LockColumn> columns = new();
    private int activeColumnIndex = 0;

    [SerializeField] private RectTransform scrollViewport; // Mask area (shared)
    [SerializeField] private RectTransform centerZoneMarker; // SHARED center zone for all columns
    [SerializeField] private GameObject symbolPrefab;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text progressText;

    [Header("Visual Feedback")]
    [SerializeField] private Image feedbackImage;
    [SerializeField] private float feedbackDuration = 0.3f;

    private List<char> correctSequence = new();
    private int currentTargetIndex;
    private Coroutine rotationCoroutine;
    private bool canInput = true;

    [System.Serializable]
    public class LockColumn
    {
        public RectTransform symbolColumn;

        [NonSerialized] public List<SymbolItem> symbolItems;
    }


    // symbol item (runtime only)
    public class SymbolItem
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

        // Create symbols for ALL configured columns (they remain in scene),
        // but only the active column will rotate / accept alignment checks.
        for (int i = 0; i < columns.Count; i++)
        {
            CreateSymbolColumnForIndex(i);
        }

        // Ensure active index starts at 0
        activeColumnIndex = 0;
        UpdateProgress();
        StartRotationForColumn(activeColumnIndex);

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
            char symbol = pool[UnityEngine.Random.Range(0, pool.Length)];
            correctSequence.Add(symbol);
        }

        Debug.Log($"[LockTimingPuzzle] Sequence: {string.Join("", correctSequence)}");
    }

    // === Symbol Column Creation ===

    private void CreateSymbolColumnForIndex(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= columns.Count) return;

        var column = columns[columnIndex];

        if (column.symbolColumn == null || symbolPrefab == null)
        {
            Debug.LogError($"[LockTimingPuzzle] Column {columnIndex} is missing references!");
            return;
        }

        // INIT LISTU (KRITICKÉ)
        column.symbolItems = new List<SymbolItem>();

        // clear children
        for (int i = column.symbolColumn.childCount - 1; i >= 0; i--)
            Destroy(column.symbolColumn.GetChild(i).gameObject);

        char[] pool = config.GetSymbolPool();
        int totalSymbols = config.visibleSymbolsInColumn;

        for (int i = 0; i < totalSymbols; i++)
        {
            GameObject go = Instantiate(symbolPrefab, column.symbolColumn);
            TMP_Text text = go.GetComponent<TMP_Text>();
            RectTransform rt = go.GetComponent<RectTransform>();

            char symbol = pool[UnityEngine.Random.Range(0, pool.Length)];

            var item = new SymbolItem
            {
                gameObject = go,
                text = text,
                rectTransform = rt,
                character = symbol
            };

            text.text = symbol.ToString();
            text.color = config.neutralColor;

            // SPAWN SHORA
            rt.anchoredPosition = new Vector2(0, -i * config.symbolSpacing);

            column.symbolItems.Add(item);
        }

        Debug.Log($"[LockTimingPuzzle] Spawned {column.symbolItems.Count} symbols in column {columnIndex}");
    }


    private void ClearSymbols()
    {
        foreach (var column in columns)
        {
            if (column.symbolColumn != null)
            {
                for (int i = column.symbolColumn.childCount - 1; i >= 0; i--)
                    Destroy(column.symbolColumn.GetChild(i).gameObject);
            }

            column.symbolItems.Clear();
        }
    }

    // === Rotation Logic (per-column, infinite wraparound) ===

    private void StartRotationForColumn(int columnIndex)
    {
        StopRotation();

        if (columnIndex < 0 || columnIndex >= columns.Count) return;

        // Ensure the target symbol exists in the newly active column
        EnsureCorrectTargetExistsForColumn(columnIndex);

        rotationCoroutine = StartCoroutine(RotateSymbolsCoroutineForColumn(columnIndex));
    }

    private void StopRotation()
    {
        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
            rotationCoroutine = null;
        }
    }

    private IEnumerator RotateSymbolsCoroutineForColumn(int columnIndex)
    {
        var column = columns[columnIndex];

        while (true)
        {
            if (column.symbolItems == null || column.symbolItems.Count == 0)
            {
                yield return null;
                continue;
            }


            float moveSpeed = config.rotationSpeed * config.symbolSpacing * Time.deltaTime;
            char currentTarget = correctSequence[Mathf.Clamp(currentTargetIndex, 0, correctSequence.Count - 1)];

            for (int i = 0; i < column.symbolItems.Count; i++)
            {
                var item = column.symbolItems[i];
                Vector2 pos = item.rectTransform.anchoredPosition;

                // Move DOWN (scrolling down visually)
                pos.y -= moveSpeed;

                // Wraparound: if symbol goes below bottom, teleport to top
                float viewportHeight = config.visibleSymbolsInColumn * config.symbolSpacing;
                float bottomThreshold = -viewportHeight;

                if (pos.y < bottomThreshold)
                {
                    pos.y += viewportHeight;
                }

                item.rectTransform.anchoredPosition = pos;

                // Highlight ONLY the correct target symbol (green) for this column
                bool isTargetSymbol = item.character == currentTarget;
                item.text.color = isTargetSymbol ? config.targetColor : config.neutralColor;
            }

            yield return null;
        }
    }

    private void EnsureCorrectTargetExistsForColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= columns.Count) return;

        var column = columns[columnIndex];
        char target = correctSequence[Mathf.Clamp(currentTargetIndex, 0, correctSequence.Count - 1)];
        bool hasTarget = false;

        foreach (var item in column.symbolItems)
        {
            if (item.character == target)
            {
                hasTarget = true;
                break;
            }
        }

        // If target missing, replace a random symbol
        if (!hasTarget && column.symbolItems.Count > 0)
        {
            var randomItem = column.symbolItems[UnityEngine.Random.Range(0, column.symbolItems.Count)];
            randomItem.character = target;
            randomItem.text.text = target.ToString();
        }
    }

    // === Input Handling ===

    private void OnSubmitPressed()
    {
        if (!isActive || !canInput) return;

        char targetSymbol = correctSequence[Mathf.Clamp(currentTargetIndex, 0, correctSequence.Count - 1)];
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
        // Use active column's items with shared center zone marker
        if (activeColumnIndex < 0 || activeColumnIndex >= columns.Count)
        {
            Debug.LogError("[LockTimingPuzzle] Active column index out of range!");
            return false;
        }

        var column = columns[activeColumnIndex];

        if (centerZoneMarker == null)
        {
            Debug.LogError("[LockTimingPuzzle] Shared center zone marker is missing!");
            return false;
        }

        // Convert center zone position to world space, then to the symbol column's local space
        Vector3 centerWorldPos = centerZoneMarker.TransformPoint(Vector3.zero);
        Vector3 centerLocalToColumn = column.symbolColumn.InverseTransformPoint(centerWorldPos);
        float centerY = centerLocalToColumn.y;

        Debug.Log($"[LockTimingPuzzle] Column {activeColumnIndex} | Center zone Y in column space: {centerY:F1}, Tolerance: {config.alignmentTolerance}");

        // Find symbol closest to center zone
        float closestDistance = float.MaxValue;
        SymbolItem closestItem = null;

        foreach (var item in column.symbolItems)
        {
            float symbolY = item.rectTransform.anchoredPosition.y;
            float distance = Mathf.Abs(symbolY - centerY);

            Debug.Log($"  Symbol '{item.character}' at Y={symbolY:F1}, distance={distance:F1}");

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        // Check if within tolerance AND correct symbol
        bool inZone = closestDistance <= config.alignmentTolerance;
        bool correctChar = closestItem != null && closestItem.character == targetSymbol;

        Debug.Log($"[LockTimingPuzzle] Closest: '{closestItem?.character}' (dist={closestDistance:F1}), " +
                  $"Target: '{targetSymbol}', InZone: {inZone}, CorrectChar: {correctChar}");

        return inZone && correctChar;
    }

    private void OnCorrectInput()
    {
        Debug.Log($"[LockTimingPuzzle] ✓ Correct! Column {activeColumnIndex + 1} unlocked!");

        StopRotation();
        ShowFeedback(config.targetColor);

        activeColumnIndex++;
        currentTargetIndex++;

        // Complete if we've satisfied the configured sequence length or ran out of columns
        if (currentTargetIndex >= config.sequenceLength || activeColumnIndex >= columns.Count)
        {
            CompletePuzzle();
        }
        else
        {
            UpdateProgress();
            StartRotationForColumn(activeColumnIndex); // Start next column
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
        {
            // show current symbol progress and active column info
            progressText.text = $"{currentTargetIndex}/{config.sequenceLength}";
        }
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