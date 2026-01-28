using System.Collections;
using UnityEngine;

public class UIPanelManager : MonoBehaviour
{
    public float switchDelay = 0.3f;
    public Animator animator;

    public CameraPanelAnimation cameraMove; // reference na script kamery

    public CanvasGroup panelMenu;
    public CanvasGroup panelSettings;
    public CanvasGroup panelControls;
    public CanvasGroup panelClass;

    private void Start()
    {
        panelMenu.alpha = 1;
        panelSettings.alpha = 0;
        panelControls.alpha = 0;
        panelClass.alpha = 0;
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

        if (target == 3)
        {
            cameraMove.MoveForward();
        }
        else
        {
            cameraMove.MoveBack();
        }
    }
}
