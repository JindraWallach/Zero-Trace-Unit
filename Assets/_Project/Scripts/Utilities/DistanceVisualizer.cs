#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class DistanceVisualizer : MonoBehaviour
{
    public Transform pivot;

    void OnDrawGizmos()
    {
        if (pivot == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, pivot.position);
        Gizmos.DrawSphere(transform.position, 0.05f);
        Gizmos.DrawSphere(pivot.position, 0.05f);

        // Vzdálenost
        float distance = Vector3.Distance(transform.position, pivot.position);

#if UNITY_EDITOR
        // Popisek do Scene view
        Vector3 midPoint = (transform.position + pivot.position) / 2f;
        Handles.Label(midPoint + Vector3.up * 0.1f, $"{distance:F2} m");
#endif
    }
}
