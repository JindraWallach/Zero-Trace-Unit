using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ordered list of objectives for one level.
/// SRP: Data container for level mission sequence only.
/// OCP: Add/reorder objectives in Inspector - zero code changes needed.
/// Create via: Assets > Create > Zero Trace > Mission > Mission Data
/// </summary>
[CreateAssetMenu(fileName = "MissionData_Level1", menuName = "Zero Trace/Mission/Mission Data")]
public class MissionDataSO : ScriptableObject
{
    [Header("Level Objectives (in order)")]
    [Tooltip("Index 0 auto-activates on level start")]
    public List<MissionObjectiveSO> objectives = new List<MissionObjectiveSO>();

    private void OnValidate()
    {
        if (objectives == null || objectives.Count == 0)
            Debug.LogWarning($"[MissionDataSO] '{name}' has no objectives assigned!", this);
    }
}