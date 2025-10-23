using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathRowsPuzzle : MonoBehaviour, IPuzzle, IInitializable
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
    public event Action OnPuzzleCancelled;

    private List<int> targets = new List<int>();    
    private List<MathRowUI> cells = new List<MathRowUI>();
    private InputReader inputReader;
    private bool[] solved;

    [Header("Behaviour")]
    public bool generateOnStart = true;

    private void Start()
    {
        if (generateOnStart) Generate();
    }

    public void Initialize(DependencyInjector dependencyInjector)
    {
        inputReader = dependencyInjector.InputReader;
        inputReader.onEscapePressed += CancelPuzzle;
    }

    private void OnValidate()
    {
        // ensure rows is at least 1 even if user types 0 or negative in Inspector
        if (rows < 1) rows = 1;
    }

    private void CancelPuzzle()
    {
        Debug.Log("MathRowsPuzzle: Puzzle cancelled by player.");
        OnPuzzleCancelled?.Invoke();
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
            // generate a reasonable random expression (always an operation, no plain integers)
            expressions.Add(GenerateRandomExpression());
        }
    }

    // Generates expressions that the evaluator supports:
    // - "a + b"  (allows negative operands)
    // - "a - b"  (both operands non-negative to avoid parser edge-cases)
    // - "a % b"  (b != 0)
    // - "|a - b|" (absolute difference)
    private string GenerateRandomExpression()
    {
        // select expression kind
        int kind = UnityEngine.Random.Range(0, 4);
        int a, b;

        switch (kind)
        {
            case 0: // plus (possibly negative operand)
                a = UnityEngine.Random.Range(-5, 21); // allow some negatives
                b = UnityEngine.Random.Range(0, 21);
                return $"|{a} + {b}|";

            case 1: // minus (avoid leading negative literal which the simple parser can't handle)
                a = UnityEngine.Random.Range(0, 21);
                b = UnityEngine.Random.Range(0, 21);
                return $"|{a} - {b}|";

            case 2: // modulo (ensure divisor != 0)
                a = UnityEngine.Random.Range(0, 50);
                b = UnityEngine.Random.Range(1, 10);
                return $"|{a} % {b}|";

            default: // absolute difference
                a = UnityEngine.Random.Range(0, 21);
                b = UnityEngine.Random.Range(0, 21);
                return $"|{a} - {b}|";
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
            CancelPuzzle();
        }
    }

    private void CheckAllSolved()
    {
        foreach (var s in solved) if (!s) return;
        OnPuzzleSuccess?.Invoke();
    }

    // Very small evaluator supporting: +, -, %, single absolute patterns like |a - b| or plain ints.
    // Modified: always take absolute value of the computed result before normalizing to a digit.
    private int EvaluateToDigit(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0;
        expr = expr.Trim();

        try
        {
            int result;

            // If expression is an explicit absolute wrapper, evaluate inner and take abs (kept for clarity).
            if (expr.StartsWith("|") && expr.EndsWith("|"))
            {
                string inner = expr.Substring(1, expr.Length - 2).Trim();
                result = EvaluateSimple(inner);
                result = Math.Abs(result);
            }
            else
            {
                result = EvaluateSimple(expr);
                // Always use absolute value as requested.
                result = Math.Abs(result);
            }

            return NormalizeToDigit(result);
        }
        catch
        {
            return 0;
        }
    }

    // supports "a + b", "a - b", "a % b" or single int
    // Improved minus parsing to correctly handle leading negative literals (e.g. "-3-2" or "-3")
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
            // Find the binary minus operator taking into account a possible leading negative sign.
            int idx;
            if (input.StartsWith("-"))
            {
                // look for next '-' after the leading one
                idx = input.IndexOf('-', 1);
                if (idx == -1)
                {
                    // no binary minus, it's a single negative number
                    return int.Parse(input);
                }
            }
            else
            {
                idx = input.IndexOf('-');
            }

            // left may include a leading '-' (negative literal), right is remainder after the operator
            string left = input.Substring(0, idx);
            string right = input.Substring(idx + 1);
            int a = int.Parse(left);
            int b = int.Parse(right);
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