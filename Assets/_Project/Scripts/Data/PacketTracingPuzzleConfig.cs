using UnityEngine;

/// <summary>
/// ScriptableObject configuration for packet tracing puzzle.
/// Defines grid size, node count, visual settings.
/// Create via: Assets > Create > Zero Trace > Packet Tracing Puzzle Config
/// </summary>
[CreateAssetMenu(fileName = "PacketTracingPuzzleConfig", menuName = "Zero Trace/Packet Tracing Puzzle Config")]
public class PacketTracingPuzzleConfig : ScriptableObject
{
    [Header("Grid Settings")]
    [Tooltip("Grid columns")]
    [Range(2, 5)]
    public int gridColumns = 3;

    [Tooltip("Grid rows")]
    [Range(2, 5)]
    public int gridRows = 3;

    [Tooltip("Spacing between nodes (pixels)")]
    [Range(50f, 200f)]
    public float nodeSpacing = 100f;

    [Header("Node Settings")]
    [Tooltip("Start node position (0 = top-left, gridColumns*gridRows-1 = bottom-right)")]
    public int startNodeIndex = 0;

    [Tooltip("Goal node position")]
    public int goalNodeIndex = 8;

    [Tooltip("Disabled node indices (obstacles)")]
    public int[] disabledNodes = new int[] { 4 };

    [Header("Solution")]
    [Tooltip("Correct path (node indices). Example: [0,1,2,5,8] means START→1→2→5→GOAL")]
    public int[] correctPath = new int[] { 0, 1, 2, 5, 8 };

    [Header("Visual Feedback")]
    [Tooltip("Color for unconnected nodes")]
    public Color neutralColor = Color.white;

    [Tooltip("Color for connected nodes")]
    public Color connectedColor = new Color(0.2f, 0.8f, 1f); // Cyan

    [Tooltip("Color for start node")]
    public Color startColor = Color.green;

    [Tooltip("Color for goal node")]
    public Color goalColor = new Color(1f, 0.8f, 0f); // Yellow

    [Tooltip("Color for disabled nodes")]
    public Color disabledColor = new Color(0.3f, 0.3f, 0.3f); // Gray

    [Tooltip("Color for connection lines")]
    public Color lineColor = new Color(0.2f, 1f, 0.2f); // Green

    [Tooltip("Color for wrong connection")]
    public Color errorColor = Color.red;

    [Tooltip("Line width")]
    [Range(2f, 10f)]
    public float lineWidth = 4f;

    [Header("Debug")]
    [Tooltip("Show correct path in console")]
    public bool debugShowSolution = false;

    private void OnValidate()
    {
        // Clamp indices
        int maxIndex = gridColumns * gridRows - 1;
        startNodeIndex = Mathf.Clamp(startNodeIndex, 0, maxIndex);
        goalNodeIndex = Mathf.Clamp(goalNodeIndex, 0, maxIndex);

        // Validate disabled nodes
        if (disabledNodes != null)
        {
            for (int i = 0; i < disabledNodes.Length; i++)
            {
                disabledNodes[i] = Mathf.Clamp(disabledNodes[i], 0, maxIndex);

                // Warn if START/GOAL is disabled
                if (disabledNodes[i] == startNodeIndex || disabledNodes[i] == goalNodeIndex)
                {
                    Debug.LogWarning($"[PacketTracingPuzzleConfig] Disabled node {disabledNodes[i]} conflicts with START/GOAL!");
                }
            }
        }

        // Validate correct path
        if (correctPath != null && correctPath.Length > 0)
        {
            if (correctPath[0] != startNodeIndex)
            {
                Debug.LogWarning($"[PacketTracingPuzzleConfig] Correct path should start with START node ({startNodeIndex})!");
            }

            if (correctPath[correctPath.Length - 1] != goalNodeIndex)
            {
                Debug.LogWarning($"[PacketTracingPuzzleConfig] Correct path should end with GOAL node ({goalNodeIndex})!");
            }
        }
    }

    /// <summary>
    /// Check if node is disabled.
    /// </summary>
    public bool IsNodeDisabled(int index)
    {
        if (disabledNodes == null) return false;

        foreach (int disabled in disabledNodes)
        {
            if (disabled == index)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if two nodes are neighbors (adjacent in grid).
    /// </summary>
    public bool AreNeighbors(int indexA, int indexB)
    {
        int rowA = indexA / gridColumns;
        int colA = indexA % gridColumns;

        int rowB = indexB / gridColumns;
        int colB = indexB % gridColumns;

        // Adjacent horizontally or vertically (not diagonal)
        bool horizontal = (Mathf.Abs(colA - colB) == 1) && (rowA == rowB);
        bool vertical = (Mathf.Abs(rowA - rowB) == 1) && (colA == colB);

        return horizontal || vertical;
    }

    /// <summary>
    /// Get grid position (row, col) from index.
    /// </summary>
    public Vector2Int GetGridPosition(int index)
    {
        int row = index / gridColumns;
        int col = index % gridColumns;
        return new Vector2Int(col, row);
    }
}