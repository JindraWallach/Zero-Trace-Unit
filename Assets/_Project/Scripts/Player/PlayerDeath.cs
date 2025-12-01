using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player death execution: ragdoll, camera effects, scene reload.
/// Pure execution component - triggered by GameManager.
/// SRP: Only handles death sequence, doesn't decide when to die.
/// 
/// CRITICAL: Disables SamplePlayerAnimationController to prevent 
/// "CharacterController.Move called on inactive controller" error.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerDeath : MonoBehaviour
{
    [Header("Ragdoll")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody[] ragdollRigidbodies;

    [Header("Death Effects")]
    [SerializeField] private bool enableSlowMotion = true;
    [SerializeField] private float slowMotionScale = 0.3f;
    [SerializeField] private float slowMotionDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool isDead;

    private CharacterController characterController;
    private Coroutine deathSequenceCoroutine;

    // Cache player controller reference
    private Synty.AnimationBaseLocomotion.Samples.SamplePlayerAnimationController playerController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<Synty.AnimationBaseLocomotion.Samples.SamplePlayerAnimationController>();

        if (animator == null)
            animator = GetComponent<Animator>();

        SetRagdollEnabled(false);
        isDead = false;
    }

    /// <summary>
    /// Execute death sequence with optional delay before scene reload.
    /// Called by GameManager.OnPlayerCaught().
    /// NOTE: GameManager should call InputReader.DisableInputs() BEFORE this method.
    /// </summary>
    /// <param name="reloadDelay">Delay in seconds before reloading scene</param>
    public void ExecuteDeath(float reloadDelay = 2f)
    {
        if (isDead)
            return; // Already dead

        isDead = true;

        // Stop any existing sequence
        if (deathSequenceCoroutine != null)
            StopCoroutine(deathSequenceCoroutine);

        deathSequenceCoroutine = StartCoroutine(DeathSequence(reloadDelay));
    }

    private IEnumerator DeathSequence(float reloadDelay)
    {
        Debug.Log("[PlayerDeath] Death sequence started");

        // Step 1: CRITICAL - Disable SamplePlayerAnimationController FIRST
        // This prevents "CharacterController.Move called on inactive controller" error
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("[PlayerDeath] SamplePlayerAnimationController disabled");
        }

        // Step 2: Disable character controller (now safe - nothing calls Move())
        if (characterController != null)
            characterController.enabled = false;

        // Step 3: Disable animator (stop walk/run animations)
        if (animator != null)
            animator.enabled = false;

        // Step 4: Enable ragdoll physics
        SetRagdollEnabled(true);

        // Step 5: Optional slow-motion effect
        if (enableSlowMotion)
        {
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = 0.02f * slowMotionScale; // Keep physics stable

            yield return new WaitForSecondsRealtime(slowMotionDuration);

            // Restore normal time (for UI/reload)
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        // Step 6: Wait before reload
        float remainingDelay = reloadDelay - (enableSlowMotion ? slowMotionDuration : 0f);
        if (remainingDelay > 0f)
            yield return new WaitForSecondsRealtime(remainingDelay);

        // Step 7: Reload scene
        Debug.Log("[PlayerDeath] Reloading scene");

        if (SceneManager.Instance != null)
            SceneManager.Instance.ReloadCurrentScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
    }

    private void SetRagdollEnabled(bool enabled)
    {
        if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
        {
            Debug.LogWarning("[PlayerDeath] No ragdoll rigidbodies assigned!");
            return;
        }

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = !enabled;
                rb.detectCollisions = enabled;
            }
        }

        Debug.Log($"[PlayerDeath] Ragdoll {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Manual death trigger for testing or traps.
    /// </summary>
    public void Die()
    {
        ExecuteDeath(2f);
    }

    /// <summary>
    /// Check if player is currently dead.
    /// </summary>
    public bool IsDead => isDead;

    private void OnDestroy()
    {
        // Ensure time scale is reset if object destroyed during death
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}