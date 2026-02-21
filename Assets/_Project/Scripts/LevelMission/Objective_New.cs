using UnityEngine;

/// <summary>
/// Single mission objective data container.
/// SRP: Holds display data for one objective only.
/// OCP: Add new objectives in MissionDataSO without touching code.
/// Create via: Assets > Create > Zero Trace > Mission > Objective
/// </summary>
[CreateAssetMenu(fileName = "Objective_New", menuName = "Zero Trace/Mission/Objective")]
public class MissionObjectiveSO : ScriptableObject
{
    [Header("Objective Info")]
    [Tooltip("Text shown to player in HUD popup and Pause Menu")]
    public string objectiveText = "Complete the objective";

    [Tooltip("Optional description for Pause Menu")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Display")]
    [Tooltip("How long the HUD popup stays on screen (seconds)")]
    [Range(1f, 6f)]
    public float popupDuration = 3f;
}