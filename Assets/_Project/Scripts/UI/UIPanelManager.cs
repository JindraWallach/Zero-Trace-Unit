using System.Collections;
using UnityEngine;

public class UIPanelManager : MonoBehaviour
{

    public float switchDelay = 0.3f;
    public Animator animator;
    public void SwitchToPanel(int target)
    {
        StartCoroutine(SwitchPanelCoroutine(target));
    }

    private IEnumerator SwitchPanelCoroutine(int target)
    {
        animator.SetInteger("CurrentPanel", -1); 
        yield return new WaitForSeconds(switchDelay);  
        animator.SetInteger("CurrentPanel", target); 
    }

}
