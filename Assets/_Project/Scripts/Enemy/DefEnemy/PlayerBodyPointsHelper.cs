using UnityEngine;

/// <summary>
/// Helper component for player - holds references to body points.
/// Makes it easy for enemies to find detection points.
/// Attach this to Player root and assign transforms in Inspector.
/// </summary>
public class PlayerBodyPointsHelper : MonoBehaviour
{
    [Header("Detection Points for Enemy AI")]
    [Tooltip("Head transform (highest detection priority)")]
    public Transform headPoint;

    [Tooltip("Torso/chest transform (main body mass)")]
    public Transform torsoPoint;

    [Tooltip("Left hand transform (for partial visibility)")]
    public Transform leftHandPoint;

    [Tooltip("Right hand transform (for partial visibility)")]
    public Transform rightHandPoint;

    [Header("Auto-Setup")]
    [Tooltip("Automatically find body points on Start by searching hierarchy")]
    [SerializeField] private bool autoFindOnStart = true;

    [Header("Debug Visualization")]
    [Tooltip("Show body points as colored spheres in Scene view")]
    [SerializeField] private bool showGizmos = true;

    private void Start()
    {
        if (autoFindOnStart)
        {
            AutoFindBodyPoints();
        }

        ValidateSetup();
    }

    /// <summary>
    /// Automatically finds body points by searching hierarchy.
    /// Searches for common bone names in player rig.
    /// </summary>
    [ContextMenu("Auto-Find Body Points")]
    public void AutoFindBodyPoints()
    {
        // Search patterns for each point
        string[] headNames = { "Head", "head", "Bip_Head", "mixamorig:Head" };
        string[] torsoNames = { "Spine1", "spine1", "Chest", "chest", "Bip_Spine1" };
        string[] leftHandNames = { "LeftHand", "lefthand", "L_Hand", "hand_l", "mixamorig:LeftHand" };
        string[] rightHandNames = { "RightHand", "righthand", "R_Hand", "hand_r", "mixamorig:RightHand" };

        // Find head
        if (headPoint == null)
            headPoint = FindTransformByNames(headNames);

        // Find torso
        if (torsoPoint == null)
            torsoPoint = FindTransformByNames(torsoNames);

        // Find left hand
        if (leftHandPoint == null)
            leftHandPoint = FindTransformByNames(leftHandNames);

        // Find right hand
        if (rightHandPoint == null)
            rightHandPoint = FindTransformByNames(rightHandNames);

        // Log results
        Debug.Log($"[PlayerBodyPointsHelper] Auto-find results:\n" +
                  $"  Head: {(headPoint ? headPoint.name : "NOT FOUND")}\n" +
                  $"  Torso: {(torsoPoint ? torsoPoint.name : "NOT FOUND")}\n" +
                  $"  Left Hand: {(leftHandPoint ? leftHandPoint.name : "NOT FOUND")}\n" +
                  $"  Right Hand: {(rightHandPoint ? rightHandPoint.name : "NOT FOUND")}");
    }

    /// <summary>
    /// Get all body points as array (for easy enemy access).
    /// </summary>
    public Transform[] GetBodyPoints()
    {
        return new Transform[]
        {
            headPoint,
            torsoPoint,
            leftHandPoint,
            rightHandPoint
        };
    }

    /// <summary>
    /// Get body point by index (0=head, 1=torso, 2=lefthand, 3=righthand).
    /// </summary>
    public Transform GetBodyPoint(int index)
    {
        switch (index)
        {
            case 0: return headPoint;
            case 1: return torsoPoint;
            case 2: return leftHandPoint;
            case 3: return rightHandPoint;
            default: return null;
        }
    }

    private Transform FindTransformByNames(string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            Transform found = FindChildRecursive(transform, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string nameToFind)
    {
        // Exact match
        if (parent.name.Equals(nameToFind, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        // Contains match
        if (parent.name.IndexOf(nameToFind, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return parent;

        // Search children
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, nameToFind);
            if (result != null)
                return result;
        }

        return null;
    }

    private void ValidateSetup()
    {
        int validPoints = 0;

        if (headPoint != null) validPoints++;
        if (torsoPoint != null) validPoints++;
        if (leftHandPoint != null) validPoints++;
        if (rightHandPoint != null) validPoints++;

        if (validPoints == 0)
        {
            Debug.LogError("[PlayerBodyPointsHelper] No body points assigned! Enemy detection won't work. Run 'Auto-Find Body Points' from context menu.", this);
        }
        else if (validPoints < 4)
        {
            Debug.LogWarning($"[PlayerBodyPointsHelper] Only {validPoints}/4 body points assigned. Detection accuracy will be reduced.", this);
        }
        else
        {
            Debug.Log($"[PlayerBodyPointsHelper] All 4 body points assigned successfully! ✓", this);
        }
    }

    // === DEBUG GIZMOS ===

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        // Draw body points as colored spheres
        if (headPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(headPoint.position, 0.15f);
            UnityEditor.Handles.Label(headPoint.position + Vector3.up * 0.25f, "HEAD", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.red },
                fontSize = 10,
                fontStyle = FontStyle.Bold
            });
        }

        if (torsoPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(torsoPoint.position, 0.2f);
            UnityEditor.Handles.Label(torsoPoint.position + Vector3.up * 0.25f, "TORSO", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.yellow },
                fontSize = 10,
                fontStyle = FontStyle.Bold
            });
        }

        if (leftHandPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftHandPoint.position, 0.12f);
            UnityEditor.Handles.Label(leftHandPoint.position + Vector3.up * 0.2f, "L", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.cyan },
                fontSize = 9
            });
        }

        if (rightHandPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(rightHandPoint.position, 0.12f);
            UnityEditor.Handles.Label(rightHandPoint.position + Vector3.up * 0.2f, "R", new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.magenta },
                fontSize = 9
            });
        }

        // Draw lines connecting points (skeleton visualization)
        if (headPoint != null && torsoPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(headPoint.position, torsoPoint.position);
        }

        if (torsoPoint != null && leftHandPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(torsoPoint.position, leftHandPoint.position);
        }

        if (torsoPoint != null && rightHandPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(torsoPoint.position, rightHandPoint.position);
        }
    }
#endif

    // === CONTEXT MENU HELPERS ===

#if UNITY_EDITOR
    [ContextMenu("Clear All Body Points")]
    private void ClearAllBodyPoints()
    {
        headPoint = null;
        torsoPoint = null;
        leftHandPoint = null;
        rightHandPoint = null;
        Debug.Log("[PlayerBodyPointsHelper] Cleared all body points");
    }

    [ContextMenu("Validate Setup")]
    private void ValidateSetupMenu()
    {
        ValidateSetup();
    }
#endif
}