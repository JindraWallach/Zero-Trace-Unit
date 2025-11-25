using System.Collections;
using Unity.AppUI.UI;
using UnityEngine;

public class UIPanelManager : MonoBehaviour
{

    public float switchDelay = 0.3f;
    public Animator animator;
    public CanvasGroup panelMenu;
    public CanvasGroup panelSettings;
    public CanvasGroup panelControls;

    private void Start()
    {
        panelMenu.alpha = 1;
        panelSettings.alpha = 0;
        panelControls.alpha = 0;
    }

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
