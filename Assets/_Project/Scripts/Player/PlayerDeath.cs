using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player death execution: ragdoll, force application, scene reload.
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
    /// Execute death with ragdoll force applied.
    /// Called by GameManager after taser effect spawned.
    /// </summary>
    public void ExecuteDeathWithForce(Vector3 forceDirection, float forceMagnitude, float reloadDelay)
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[PlayerDeath] Executing death with force: {forceDirection} x {forceMagnitude}");

        // CRITICAL: Disable SamplePlayerAnimationController FIRST
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("[PlayerDeath] SamplePlayerAnimationController disabled");
        }

        // Disable character controller (now safe)
        if (characterController != null)
            characterController.enabled = false;

        // Disable animator
        if (animator != null)
            animator.enabled = false;

        // Enable ragdoll
        SetRagdollEnabled(true);

        // Apply force to torso
        Rigidbody torso = GetTorsoRigidbody();
        if (torso != null)
        {
            torso.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
            Debug.Log($"[PlayerDeath] Force applied to {torso.name}");
        }
        else
        {
            Debug.LogWarning("[PlayerDeath] Torso rigidbody not found!");
        }

        // Start reload sequence
        if (deathSequenceCoroutine != null)
            StopCoroutine(deathSequenceCoroutine);

        deathSequenceCoroutine = StartCoroutine(DeathSequence(reloadDelay));
    }

    private IEnumerator DeathSequence(float reloadDelay)
    {
        Debug.Log("[PlayerDeath] Death sequence started");

        // Optional slow-motion effect
        if (enableSlowMotion)
        {
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = 0.02f * slowMotionScale;

            yield return new WaitForSecondsRealtime(slowMotionDuration);

            // Restore normal time
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        // Wait before reload
        float remainingDelay = reloadDelay - (enableSlowMotion ? slowMotionDuration : 0f);
        if (remainingDelay > 0f)
            yield return new WaitForSecondsRealtime(remainingDelay);

        // Reload scene
        Debug.Log("[PlayerDeath] Reloading scene");

        if (SceneManager.Instance != null)
            SceneManager.Instance.ReloadCurrentScene();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
    }

    /// <summary>
    /// Find torso rigidbody for force application.
    /// Searches for Spine/Chest/Torso by name.
    /// </summary>
    private Rigidbody GetTorsoRigidbody()
    {
        if (ragdollRigidbodies == null || ragdollRigidbodies.Length == 0)
        {
            Debug.LogWarning("[PlayerDeath] No ragdoll rigidbodies assigned!");
            return null;
        }

        // Search for torso by name
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            string name = rb.name.ToLower();
            if (name.Contains("spine") || name.Contains("chest") || name.Contains("torso"))
            {
                Debug.Log($"[PlayerDeath] Found torso: {rb.name}");
                return rb;
            }
        }

        // Fallback: use first rigidbody
        Debug.LogWarning("[PlayerDeath] Torso not found by name, using first rigidbody");
        return ragdollRigidbodies[0];
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
    /// Check if player is currently dead.
    /// </summary>
    public bool IsDead => isDead;

    private void OnDestroy()
    {
        // Ensure time scale is reset
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}