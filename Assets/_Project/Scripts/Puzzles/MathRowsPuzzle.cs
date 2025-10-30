using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Math puzzle: solve expressions to unlock.
/// Refactored to extend PuzzleBase with SRP principles.
/// </summary>
public class MathRowsPuzzle : PuzzleBase, IInitializable
{
    [Header("Puzzle Config")]
    [Min(1)][SerializeField] private int rows = 5;
    [SerializeField] private List<string> expressions = new();

    [Header("UI")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject digitButtonPrefab;

    private List<int> targets = new();
    private List<MathRowUI> cells = new();
    private bool[] solved;

    private DependencyInjector dependencyInjector;

    public void Initialize(DependencyInjector dependencyInjector)
    {
        this.dependencyInjector = dependencyInjector;
        // Subscribe to ESC key via InputReader
        Debug.Log("[MathRowsPuzzle] Subscribing to onEscapePressed");
        dependencyInjector.InputReader.onEscapePressed += CancelPuzzle;
    }

    private void OnDestroy()
    {
        if (dependencyInjector?.InputReader != null)
            dependencyInjector.InputReader.onEscapePressed -= CancelPuzzle;
    }

    protected override void OnPuzzleStart()
    {
        GenerateRows();
    }

    protected override void OnPuzzleCancel()
    {
        ClearRows();
    }

    private void GenerateRows()
    {
        ClearRows();

        EnsureExpressions(rows);
        solved = new bool[rows];
        targets.Clear();
        cells.Clear();

        for (int i = 0; i < rows; i++)
        {
            string expr = expressions[i];
            int target = EvaluateToDigit(expr);
            targets.Add(target);

            var go = Instantiate(cellPrefab, gridParent);
            var ui = go.GetComponent<MathRowUI>();
            if (ui == null)
            {
                Debug.LogError("[MathRowsPuzzle] cellPrefab missing MathRowUI component");
                continue;
            }

            ui.Setup(i, expr, digitButtonPrefab, OnCellSubmitted);
            cells.Add(ui);
        }
    }

    private void ClearRows()
    {
        if (gridParent != null)
        {
            for (int i = gridParent.childCount - 1; i >= 0; i--)
                Destroy(gridParent.GetChild(i).gameObject);
        }

        cells.Clear();
    }

    private void OnCellSubmitted(int index, int digit)
    {
        if (index < 0 || index >= targets.Count || solved[index]) return;

        if (digit == targets[index])
        {
            solved[index] = true;
            cells[index].MarkSolved();
            CheckAllSolved();
        }
        else
        {
            FailPuzzle();
        }
    }

    private void CheckAllSolved()
    {
        foreach (var s in solved)
            if (!s) return;

        CompletePuzzle();
    }

    // === Expression Generation & Evaluation ===
    private void EnsureExpressions(int count)
    {
        if (expressions == null) expressions = new List<string>();

        while (expressions.Count < count)
            expressions.Add(GenerateRandomExpression());
    }

    private string GenerateRandomExpression()
    {
        int kind = UnityEngine.Random.Range(0, 4);
        int a, b;

        switch (kind)
        {
            case 0: // addition
                a = UnityEngine.Random.Range(-5, 21);
                b = UnityEngine.Random.Range(0, 21);
                return $"|{a} + {b}|";

            case 1: // subtraction
                a = UnityEngine.Random.Range(0, 21);
                b = UnityEngine.Random.Range(0, 21);
                return $"|{a} - {b}|";

            case 2: // modulo
                a = UnityEngine.Random.Range(0, 50);
                b = UnityEngine.Random.Range(1, 10);
                return $"|{a} % {b}|";

            default: // absolute difference
                a = UnityEngine.Random.Range(0, 21);
                b = UnityEngine.Random.Range(0, 21);
                return $"|{a} - {b}|";
        }
    }

    private int EvaluateToDigit(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0;

        expr = expr.Trim();

        try
        {
            int result;

            if (expr.StartsWith("|") && expr.EndsWith("|"))
            {
                string inner = expr.Substring(1, expr.Length - 2).Trim();
                result = EvaluateSimple(inner);
                result = Math.Abs(result);
            }
            else
            {
                result = EvaluateSimple(expr);
                result = Math.Abs(result);
            }

            return NormalizeToDigit(result);
        }
        catch
        {
            return 0;
        }
    }

    private int EvaluateSimple(string input)
    {
        input = input.Replace(" ", "");

        if (input.Contains("+"))
        {
            var parts = input.Split(new[] { '+' }, 2);
            return int.Parse(parts[0]) + int.Parse(parts[1]);
        }

        if (input.Contains("%"))
        {
            var parts = input.Split(new[] { '%' }, 2);
            return int.Parse(parts[0]) % int.Parse(parts[1]);
        }

        if (input.Contains("-"))
        {
            int idx;
            if (input.StartsWith("-"))
            {
                idx = input.IndexOf('-', 1);
                if (idx == -1) return int.Parse(input);
            }
            else
            {
                idx = input.IndexOf('-');
            }

            string left = input.Substring(0, idx);
            string right = input.Substring(idx + 1);
            return int.Parse(left) - int.Parse(right);
        }

        return int.Parse(input);
    }

    private int NormalizeToDigit(int value)
    {
        return ((value % 10) + 10) % 10;
    }
}