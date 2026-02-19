// Scripts/Mission/ServerObjective.cs
using UnityEngine;

/// <summary>
/// SRP: Bridges HackableObject → MissionManager.
/// Knows nothing about alarm, UI, or escape zone.
///
/// Wire HackableObject.OnHackSuccess UnityEvent → ServerObjective.OnHackSuccess()
/// </summary>
[DisallowMultipleComponent]
public class ServerObjective : MonoBehaviour
{
    private bool _hacked;

    /// <summary>
    /// Hookup: assign this method to HackableObject's OnHackSuccess() UnityEvent.
    /// </summary>
    public void OnHackSuccess()
    {
        if (_hacked) return;
        _hacked = true;

        Debug.Log("[ServerObjective] Hack success – notifying MissionManager.");

        if (MissionManager.Instance == null)
        {
            Debug.LogError("[ServerObjective] MissionManager not found in scene!");
            return;
        }

        MissionManager.Instance.OnServerHacked(transform.position);
    }
}