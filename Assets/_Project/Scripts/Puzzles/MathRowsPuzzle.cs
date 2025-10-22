using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathRowsPuzzle : MonoBehaviour, IPuzzle
{
    [Header("Layout")]
    [Tooltip("Number of rows to generate (min 1)")]
    [Min(1)]
    public int rows = 5;

    public RectTransform gridParent; // parent for cell instances
    public GameObject cellPrefab;    // prefab with MathRowUI component
    public GameObject digitButtonPrefab; // prefab for single digit Button (with TMP child)

    [Header("Expressions (optional)")]
    // If empty, example expressions will be generated to fill rows.
    public List<string> expressions = new List<string>();

    // Public event consumers can subscribe to:
    public event Action OnPuzzleSuccess;

    private List<int> targets = new List<int>();    
    private List<MathRowUI> cells = new List<MathRowUI>();
    private bool[] solved;

    [Header("Behaviour")]
    public bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart) Generate();
    }

    private void OnValidate()
    {
        // ensure rows is at least 1 even if user types 0 or negative in Inspector
        if (rows < 1) rows = 1;
    }

    // Create rows (previously grid). Count equals rows now.
    public void Generate()
    {
        ClearExisting();

        int count = rows; // generate rows-only (not rows * cols)
        EnsureExpressions(count);
        solved = new bool[count];
        targets.Clear();
        cells.Clear();

        for (int i = 0; i < count; i++)
        {
            string expr = expressions[i];
            int target = EvaluateToDigit(expr);
            targets.Add(target);

            var go = Instantiate(cellPrefab, gridParent);
            var ui = go.GetComponent<MathRowUI>();
            if (ui == null)
                throw new Exception("cellPrefab must have MathRowUI component.");

            ui.Setup(i, expr, digitButtonPrefab, OnCellSubmitted);
            cells.Add(ui);
        }
    }

    private void EnsureExpressions(int count)
    {
        if (expressions == null) expressions = new List<string>();
        while (expressions.Count < count)
        {
            // some simple example expressions:
            int idx = expressions.Count;
            var examples = new[] { "5 + 2", "5 + 13", "|8 - 9|", "12 - 7", "7 % 4", "-3 + 8", "9 - 18", "15" };
            expressions.Add(examples[idx % examples.Length]);
        }
    }

    private void ClearExisting()
    {
        // destroy children of gridParent
        if (gridParent != null)
        {
            for (int i = gridParent.childCount - 1; i >= 0; i--)
                Destroy(gridParent.GetChild(i).gameObject);
        }
    }

    // Called by MathRowUI when player clicks a digit button
    private void OnCellSubmitted(int index, int digit)
    {
        if (index < 0 || index >= targets.Count) return;
        if (solved[index]) return;

        if (digit == targets[index])
        {
            solved[index] = true;
            cells[index].MarkSolved();
            CheckAllSolved();
        }
        else
        {
            // wrong answer — reset row or give feedback
            cells[index].FlashWrong(); // visual feedback method (optional)
        }
    }

    private void CheckAllSolved()
    {
        foreach (var s in solved) if (!s) return;
        OnPuzzleSuccess?.Invoke();
    }

    // Very small evaluator supporting: +, -, %, single absolute patterns like |a - b| or plain ints.
    private int EvaluateToDigit(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0;
        expr = expr.Trim();

        try
        {
            // absolute pattern: |a - b|
            if (expr.StartsWith("|") && expr.EndsWith("|"))
            {
                string inner = expr.Substring(1, expr.Length - 2).Trim();
                int val = EvaluateSimple(inner);
                val = Math.Abs(val);
                return NormalizeToDigit(val);
            }

            int result = EvaluateSimple(expr);
            return NormalizeToDigit(result);
        }
        catch
        {
            return 0;
        }
    }

    // supports "a + b", "a - b", "a % b" or single int
    private int EvaluateSimple(string input)
    {
        input = input.Replace(" ", "");
        if (input.Contains("+"))
        {
            var parts = input.Split('+');
            return int.Parse(parts[0]) + int.Parse(parts[1]);
        }
        if (input.Contains("%"))
        {
            var parts = input.Split('%');
            return int.Parse(parts[0]) % int.Parse(parts[1]);
        }
        if (input.Contains("-"))
        {
            // handle only binary minus, negative literal is allowed (very simple)
            var parts = input.Split(new[] { '-' }, 2);
            int a = int.Parse(parts[0]);
            int b = int.Parse(parts[1]);
            return a - b;
        }

        return int.Parse(input);
    }

    private int NormalizeToDigit(int value)
    {
        // recommended normalization: ((value % 10) + 10) % 10
        return ((value % 10) + 10) % 10;
    }

    // expose manual submit API (optional)
    public void SubmitDigit(int index, int digit) => OnCellSubmitted(index, digit);

    // optional: hide/disable puzzle
    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}