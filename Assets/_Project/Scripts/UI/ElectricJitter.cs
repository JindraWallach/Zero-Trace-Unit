using UnityEngine;

public class ElectricJitter : MonoBehaviour
{
    public float jitterStrength = 0.05f;

    private Vector3 basePos;

    void Start()
    {
        basePos = transform.localPosition;
    }

    void LateUpdate()
    {
        Vector3 offset = Random.insideUnitSphere * jitterStrength;
        transform.localPosition = basePos + offset;
    }
}
