using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Packet Tracing Puzzle: Connect nodes to create path from START to GOAL.
/// Player clicks two adjacent nodes to connect them.
/// Real-time validation: if path exists START→GOAL, puzzle completes.
/// Wrong connection (non-adjacent or disabled) = instant fail.
/// </summary>
public class PacketTracingPuzzle : PuzzleBase
{
    [Header("Config")]
    [SerializeField] private PacketTracingPuzzleConfig config;

    [Header("UI Elements")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject linePrefab; // Image with width/height for line

    [Header("Visual Feedback")]
    [SerializeField] private Image feedbackImage;
    [SerializeField] private float feedbackDuration = 0.3f;

    private List<NodeUI> nodes = new();
    private List<Connection> connections = new();
    private NodeUI selectedNode;
    private GameObject linesParent;

    private class Connection
    {
        public int nodeA;
        public int nodeB;
        public GameObject lineObject;
    }

    protected override void Awake()
    {
        base.Awake();

        // Create parent for lines (render behind nodes)
        linesParent = new GameObject("Lines");
        linesParent.transform.SetParent(gridParent, false);
        linesParent.transform.SetAsFirstSibling();
    }

    protected override void OnPuzzleStart()
    {
        if (config == null)
        {
            Debug.LogError("[PacketTracingPuzzle] Config is null!");
            FailPuzzle();
            return;
        }

        GenerateGrid();

        if (feedbackImage != null)
            feedbackImage.gameObject.SetActive(false);

        if (config.debugShowSolution)
        {
            Debug.Log($"[PacketTracingPuzzle] Solution: {string.Join(" → ", config.correctPath)}");
        }
    }

    protected override void OnPuzzleCancel()
    {
        ClearGrid();
    }

    protected override void OnPuzzleComplete()
    {
        ClearGrid();
    }

    protected override void OnPuzzleFail()
    {
        ClearGrid();
    }

    // === Grid Generation ===

    private void GenerateGrid()
    {
        ClearGrid();

        int totalNodes = config.gridColumns * config.gridRows;

        for (int i = 0; i < totalNodes; i++)
        {
            CreateNode(i);
        }

        Debug.Log($"[PacketTracingPuzzle] Generated {nodes.Count} nodes in {config.gridColumns}x{config.gridRows} grid");
    }

    private void CreateNode(int index)
    {
        Vector2Int gridPos = config.GetGridPosition(index);

        // Calculate position (centered grid)
        float offsetX = -(config.gridColumns - 1) * config.nodeSpacing * 0.5f;
        float offsetY = (config.gridRows - 1) * config.nodeSpacing * 0.5f;

        Vector2 position = new Vector2(
            offsetX + gridPos.x * config.nodeSpacing,
            offsetY - gridPos.y * config.nodeSpacing
        );

        GameObject go = Instantiate(nodePrefab, gridParent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;

        NodeUI nodeUI = go.GetComponent<NodeUI>();
        if (nodeUI == null)
        {
            Debug.LogError("[PacketTracingPuzzle] nodePrefab missing NodeUI component!");
            Destroy(go);
            return;
        }

        // Determine node type
        NodeType type = NodeType.Normal;
        bool disabled = config.IsNodeDisabled(index);

        if (index == config.startNodeIndex)
            type = NodeType.Start;
        else if (index == config.goalNodeIndex)
            type = NodeType.Goal;
        else if (disabled)
            type = NodeType.Disabled;

        Color nodeColor = type switch
        {
            NodeType.Start => config.startColor,
            NodeType.Goal => config.goalColor,
            NodeType.Disabled => config.disabledColor,
            _ => config.neutralColor
        };

        nodeUI.Setup(index, type, nodeColor, OnNodeClicked);
        nodes.Add(nodeUI);
    }

    private void ClearGrid()
    {
        foreach (var node in nodes)
        {
            if (node != null)
                Destroy(node.gameObject);
        }

        nodes.Clear();

        foreach (var conn in connections)
        {
            if (conn.lineObject != null)
                Destroy(conn.lineObject);
        }

        connections.Clear();

        selectedNode = null;
    }

    // === Node Interaction ===

    private void OnNodeClicked(NodeUI clickedNode)
    {
        if (!isActive) return;

        // Ignore disabled nodes
        if (clickedNode.Type == NodeType.Disabled)
        {
            Debug.Log("[PacketTracingPuzzle] Cannot select disabled node");
            return;
        }

        // First selection
        if (selectedNode == null)
        {
            selectedNode = clickedNode;
            clickedNode.SetSelected(true);
            Debug.Log($"[PacketTracingPuzzle] Selected node {clickedNode.Index}");
            return;
        }

        // Second selection (same node = deselect)
        if (selectedNode == clickedNode)
        {
            selectedNode.SetSelected(false);
            selectedNode = null;
            Debug.Log("[PacketTracingPuzzle] Deselected node");
            return;
        }

        // Second selection (different node = try connect)
        AttemptConnection(selectedNode, clickedNode);
    }

    private void AttemptConnection(NodeUI nodeA, NodeUI nodeB)
    {
        // Check if already connected
        if (AreConnected(nodeA.Index, nodeB.Index))
        {
            Debug.Log("[PacketTracingPuzzle] Nodes already connected, removing connection");
            RemoveConnection(nodeA.Index, nodeB.Index);
            nodeA.SetSelected(false);
            selectedNode = null;
            return;
        }

        // Check if neighbors
        if (!config.AreNeighbors(nodeA.Index, nodeB.Index))
        {
            Debug.Log($"[PacketTracingPuzzle] ✗ Nodes {nodeA.Index} and {nodeB.Index} are not neighbors!");
            ShowFeedback(config.errorColor);
            FailPuzzle();
            return;
        }

        // Valid connection!
        CreateConnection(nodeA.Index, nodeB.Index);
        nodeA.SetSelected(false);
        selectedNode = null;

        Debug.Log($"[PacketTracingPuzzle] ✓ Connected {nodeA.Index} → {nodeB.Index}");

        // Check if path exists
        if (PathExists())
        {
            Debug.Log("[PacketTracingPuzzle] ✓ Path found! Puzzle complete!");
            CompletePuzzle();
        }
    }

    // === Connection Management ===

    private void CreateConnection(int indexA, int indexB)
    {
        NodeUI nodeA = nodes[indexA];
        NodeUI nodeB = nodes[indexB];

        // Create line visual
        GameObject line = Instantiate(linePrefab, linesParent.transform);
        Image lineImage = line.GetComponent<Image>();
        RectTransform lineRT = line.GetComponent<RectTransform>();

        if (lineImage != null)
            lineImage.color = config.lineColor;

        // Calculate line position & rotation
        Vector2 posA = nodeA.GetComponent<RectTransform>().anchoredPosition;
        Vector2 posB = nodeB.GetComponent<RectTransform>().anchoredPosition;

        Vector2 midpoint = (posA + posB) * 0.5f;
        float distance = Vector2.Distance(posA, posB);
        float angle = Mathf.Atan2(posB.y - posA.y, posB.x - posA.x) * Mathf.Rad2Deg;

        lineRT.anchoredPosition = midpoint;
        lineRT.sizeDelta = new Vector2(distance, config.lineWidth);
        lineRT.rotation = Quaternion.Euler(0, 0, angle);

        // Store connection
        connections.Add(new Connection
        {
            nodeA = indexA,
            nodeB = indexB,
            lineObject = line
        });

        // Update node colors
        nodeA.SetConnected(true, config.connectedColor);
        nodeB.SetConnected(true, config.connectedColor);
    }

    private void RemoveConnection(int indexA, int indexB)
    {
        Connection toRemove = null;

        foreach (var conn in connections)
        {
            if ((conn.nodeA == indexA && conn.nodeB == indexB) ||
                (conn.nodeA == indexB && conn.nodeB == indexA))
            {
                toRemove = conn;
                break;
            }
        }

        if (toRemove != null)
        {
            Destroy(toRemove.lineObject);
            connections.Remove(toRemove);

            // Update node colors (check if still connected to other nodes)
            UpdateNodeConnectionState(indexA);
            UpdateNodeConnectionState(indexB);
        }
    }

    private void UpdateNodeConnectionState(int index)
    {
        bool hasConnections = false;

        foreach (var conn in connections)
        {
            if (conn.nodeA == index || conn.nodeB == index)
            {
                hasConnections = true;
                break;
            }
        }

        NodeUI node = nodes[index];
        if (!hasConnections)
        {
            // Reset to default color
            Color defaultColor = node.Type switch
            {
                NodeType.Start => config.startColor,
                NodeType.Goal => config.goalColor,
                _ => config.neutralColor
            };

            node.SetConnected(false, defaultColor);
        }
    }

    private bool AreConnected(int indexA, int indexB)
    {
        foreach (var conn in connections)
        {
            if ((conn.nodeA == indexA && conn.nodeB == indexB) ||
                (conn.nodeA == indexB && conn.nodeB == indexA))
            {
                return true;
            }
        }

        return false;
    }

    // === Path Validation (BFS) ===

    private bool PathExists()
    {
        // BFS from START to GOAL
        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();

        queue.Enqueue(config.startNodeIndex);
        visited.Add(config.startNodeIndex);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == config.goalNodeIndex)
                return true;

            // Get neighbors
            foreach (var conn in connections)
            {
                int neighbor = -1;

                if (conn.nodeA == current)
                    neighbor = conn.nodeB;
                else if (conn.nodeB == current)
                    neighbor = conn.nodeA;

                if (neighbor != -1 && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return false;
    }

    // === Visual Feedback ===

    private void ShowFeedback(Color color)
    {
        if (feedbackImage != null)
            StartCoroutine(FeedbackFlashCoroutine(color));
    }

    private System.Collections.IEnumerator FeedbackFlashCoroutine(Color color)
    {
        feedbackImage.gameObject.SetActive(true);
        feedbackImage.color = color;

        yield return new WaitForSeconds(feedbackDuration);

        feedbackImage.gameObject.SetActive(false);
    }
}