using System;
using UnityEngine;

public class SetAnimatorActiveOnDestroy : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogWarning("Animator not found on this GameObject.", this);
    }

    private void Start()
    {
        SceneManager.Instance.OnSceneLoadStarted += HandleSceneLoadStarted;
    }

    private void HandleSceneLoadStarted()
    {
        if (animator != null)
        {
            animator.SetBool("Active", true);
            Debug.Log($"SetAnimatorActiveOnDestroy: Setting animator 'Active' parameter to true on {gameObject.name}.");
        }
    }
}
