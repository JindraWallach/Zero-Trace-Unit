using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody[] ragdollRigidbodies;

    private void Start()
    {
        SetRagdollEnabled(false);
    }

    public void Die()
    {
        animator.enabled = false;

        SetRagdollEnabled(true);

        GetComponent<CharacterController>().enabled = false;
    }

    private void SetRagdollEnabled(bool enabled)
    {
        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = !enabled;
        }
    }
}