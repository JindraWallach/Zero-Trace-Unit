using UnityEngine;

/// <summary>
/// ScriptableObject configuration for lock timing puzzle.
/// Defines sequence length, rotation speed, symbol pool, and difficulty.
/// Create via: Assets > Create > Zero Trace > Lock Timing Puzzle Config
/// </summary>
[CreateAssetMenu(fileName = "LockTimingPuzzleConfig", menuName = "Zero Trace/Lock Timing Puzzle Config")]
public class LockTimingPuzzleConfig : ScriptableObject
{
    [Header("Sequence Settings")]
    [Tooltip("Number of symbols to unlock (4-10 recommended)")]
    [Range(4, 10)]
    public int sequenceLength = 5;

    [Tooltip("Number of visible symbols in rotating column")]
    [Range(5, 12)]
    public int visibleSymbolsInColumn = 8;

    [Header("Symbol Pool")]
    [Tooltip("Available symbols (0-9, A-Z)")]
    public SymbolPool symbolPool = SymbolPool.Digits;

    [Header("Rotation Settings")]
    [Tooltip("Rotation speed (symbols per second)")]
    [Range(1f, 10f)]
    public float rotationSpeed = 3f;

    [Tooltip("Distance between symbols (Unity units)")]
    [Range(50f, 150f)]
    public float symbolSpacing = 80f;

    [Header("Alignment Settings")]
    [Tooltip("Tolerance for correct alignment (0 = exact center)")]
    [Range(0f, 50f)]
    public float alignmentTolerance = 20f;

    [Header("Visual Feedback")]
    [Tooltip("Color for active/target symbol")]
    public Color targetColor = Color.green;

    [Tooltip("Color for incorrect attempt")]
    public Color errorColor = Color.red;

    [Tooltip("Color for neutral symbols")]
    public Color neutralColor = Color.white;

    [Header("Timing")]
    [Tooltip("Cooldown between button presses (seconds)")]
    [Range(0.1f, 1f)]
    public float inputCooldown = 0.3f;

    [Header("Debug")]
    [Tooltip("Show alignment zone gizmo")]
    public bool debugAlignment = true;

    private void OnValidate()
    {
        sequenceLength = Mathf.Clamp(sequenceLength, 4, 10);
        visibleSymbolsInColumn = Mathf.Max(5, visibleSymbolsInColumn);
        rotationSpeed = Mathf.Max(0.5f, rotationSpeed);
        symbolSpacing = Mathf.Max(30f, symbolSpacing);
    }

    /// <summary>
    /// Get symbol pool as character array.
    /// </summary>
    public char[] GetSymbolPool()
    {
        return symbolPool switch
        {
            SymbolPool.Digits => new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' },
            SymbolPool.UppercaseLetters => "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(),
            SymbolPool.Combined => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(),
            _ => new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }
        };
    }
}

public enum SymbolPool
{
    Digits,              // 0-9
    UppercaseLetters,    // A-Z
    Combined             // 0-9 + A-Z
}