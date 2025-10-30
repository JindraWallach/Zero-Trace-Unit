using UnityEngine;

/// <summary>
/// Makes UI always face main camera.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    private void Start() => mainCam = Camera.main;

    private void LateUpdate()
    {
        if (mainCam != null)
            transform.LookAt(transform.position + mainCam.transform.forward);
    }
}