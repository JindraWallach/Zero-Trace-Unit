using System.Collections;
using UnityEngine;

public class CameraPanelAnimation : MonoBehaviour
{
    public Vector3 forwardOffset = new Vector3(0, 0, 3f);
    public float duration = 0.6f;

    // křivka pohybu (EaseInOut default)
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 startPos;
    private Vector3 forwardPos;
    private bool isForward = false;
    private Coroutine currentRoutine;

    void Start()
    {
        startPos = transform.position;
        forwardPos = startPos + forwardOffset;
    }

    public void MoveForward()
    {
        if (isForward) return;
        StartMove(startPos, forwardPos, true);
    }

    public void MoveBack()
    {
        if (!isForward) return;
        StartMove(forwardPos, startPos, false);
    }

    private void StartMove(Vector3 from, Vector3 to, bool forwardState)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(MoveRoutine(from, to, forwardState));
    }

    private IEnumerator MoveRoutine(Vector3 from, Vector3 to, bool forwardState)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            float easedT = moveCurve.Evaluate(t); // matematický easing
            transform.position = Vector3.LerpUnclamped(from, to, easedT);

            yield return null;
        }

        transform.position = to;
        isForward = forwardState;
    }
}
