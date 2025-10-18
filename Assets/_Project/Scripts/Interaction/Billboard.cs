using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCam;

    void Start() => mainCam = Camera.main;

    void LateUpdate()
    {
        transform.LookAt(transform.position + mainCam.transform.forward);
    }
}
