using UnityEngine;
using ZeroTrace.Audio;

/// <summary>
/// Přehrává zvuk kroků z Animation Eventů.
/// SRP: pouze zvuk — noise (AI detekce) řeší NoiseEmitter zvlášť.
/// Attach na hráče i enemy.
/// </summary>
public class FootstepSoundEmitter : MonoBehaviour
{
    [SerializeField] private string walkSoundId = "footstep_walk";
    [SerializeField] private string runSoundId = "footstep_run";

    // Voláno z Animation Eventu na walk animaci
    public void OnWalkStep()
    {
        AudioManager.Instance?.Play(walkSoundId, transform.position);
    }

    // Voláno z Animation Eventu na run animaci
    public void OnRunStep()
    {
        AudioManager.Instance?.Play(runSoundId, transform.position);
    }
}