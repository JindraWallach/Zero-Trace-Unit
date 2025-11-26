using UnityEngine;

/// <summary>
/// Minimalist animation controller for enemy AI.
/// Only essential parameters: MoveSpeed (float), IsAlert (bool), Attack (trigger), Catch (trigger).
/// NO crouch, jump, strafe, lean - keep it simple unlike player's SamplePlayerAnimationController.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    private EnemyStateMachine machine;
    private Animator animator;

    // Animation parameter hashes (for performance)
    private readonly int moveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int isAlertHash = Animator.StringToHash("IsAlert");
    private readonly int attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int catchTriggerHash = Animator.StringToHash("Catch");

    // Current values (for debugging)
    [Header("Debug - Current Values")]
    [SerializeField] private float currentMoveSpeed;
    [SerializeField] private bool isAlert;

    public void Initialize(EnemyStateMachine stateMachine)
    {
        machine = stateMachine;
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError($"[EnemyAnimation] {gameObject.name} missing Animator component!", this);
            enabled = false;
            return;
        }

        // Initialize to idle
        SetMoveSpeed(0f);
        SetAlert(false);
    }

    private void Update()
    {
        // Continuously update move speed from NavMeshAgent velocity
        if (machine != null && machine.Movement != null)
        {
            float speed = machine.Movement.CurrentSpeed;
            SetMoveSpeed(speed);
        }
    }

    /// <summary>
    /// Set movement speed parameter (drives Idle → Walk → Run blend tree).
    /// 0 = Idle, 0-2 = Walk, 2+ = Run
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        currentMoveSpeed = speed;
        animator.SetFloat(moveSpeedHash, speed);
    }

    /// <summary>
    /// Set alert state (changes animation posture).
    /// True = Alert/Combat stance, False = Relaxed patrol
    /// </summary>
    public void SetAlert(bool alert)
    {
        isAlert = alert;
        //animator.SetBool(isAlertHash, alert);
    }

    /// <summary>
    /// Play attack animation (trigger).
    /// </summary>
    public void PlayAttack()
    {
        animator.SetTrigger(attackTriggerHash);
    }

    /// <summary>
    /// Play catch/grab animation (trigger).
    /// Used when catching player.
    /// </summary>
    public void PlayCatch()
    {
        animator.SetTrigger(catchTriggerHash);
    }

    /// <summary>
    /// Immediately stop all movement animations.
    /// Useful for freeze/stun effects.
    /// </summary>
    public void StopMovement()
    {
        SetMoveSpeed(0f);
    }

    /// <summary>
    /// Get current animation state info (for debugging).
    /// </summary>
    public AnimatorStateInfo GetCurrentStateInfo()
    {
        return animator.GetCurrentAnimatorStateInfo(0);
    }

    /// <summary>
    /// Check if specific animation is playing.
    /// </summary>
    public bool IsPlayingAnimation(string stateName)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }
}