// Scripts/Tutorial/TutorialTriggerZone.cs
using UnityEngine;

/// <summary>
/// SRP: Detects player entry into a physical zone and advances one specific tutorial phase.
/// 
/// Place on a GameObject with a Collider (IsTrigger = true).
/// Set phaseToAdvance to the phase this zone completes (e.g., PHASE_MOVEMENT = 0).
/// Self-deactivates after firing to prevent duplicate triggers.
/// 
/// No Update(). Physics-driven via OnTriggerEnter only.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class TutorialTriggerZone : MonoBehaviour
{
    [Header("Tutorial Phase")]
    [Tooltip("Which tutorial phase this trigger zone completes.\n" +
             "0=Movement, 1=HackMode, 2=HackServer, 3=Puzzle, 4=Stealth, 5=Escape")]
    [SerializeField] private int phaseToAdvance = 0;

    [Header("Player Detection")]
    [Tooltip("Tag used to identify the player GameObject.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.25f);

    // ── Cache ──────────────────────────────────────────────────────────────
    private bool _triggered = false;

    // ──────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Ensure collider is a trigger
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[TutorialTriggerZone] '{name}' collider is not a trigger – setting it automatically.");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;

        _triggered = true;

        TutorialMissionManager.Instance?.AdvanceFromPhase(phaseToAdvance);

        // Disable self so this never fires again
        gameObject.SetActive(false);
    }

    // ── Gizmo ──────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;
        var col = GetComponent<Collider>();

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), sphere.radius * transform.lossyScale.x);
        }

        // Label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
            $"TutorialZone\nPhase {phaseToAdvance}",
            new GUIStyle { normal = { textColor = Color.cyan }, fontSize = 11 });
    }
#endif
}