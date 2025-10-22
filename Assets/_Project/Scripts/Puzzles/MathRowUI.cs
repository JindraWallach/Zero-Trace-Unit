using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MathRowUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text expressionText;
    public Transform buttonsParent; // container where 0..9 buttons will be instantiated
    public Image solvedOverlay;     // optional overlay to show solved state

    int index;
    Action<int, int> submitCallback;

    // Setup called by MathRowsPuzzle when instantiating the cell
    public void Setup(int index, string expression, GameObject digitButtonPrefab, Action<int, int> submitCallback)
    {
        this.index = index;
        this.submitCallback = submitCallback;
        expressionText.text = expression;

        // create buttons 0..9
        for (int d = 1; d <= 9; d++)
            CreateDigitButton(d, digitButtonPrefab);
        CreateDigitButton(0, digitButtonPrefab); // 0 last (common layout)

        if (solvedOverlay != null) solvedOverlay.gameObject.SetActive(false);
    }

    private void CreateDigitButton(int digit, GameObject prefab)
    {
        var go = Instantiate(prefab, buttonsParent);
        var btn = go.GetComponent<Button>();
        var label = go.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = digit.ToString();
        btn.onClick.AddListener(() => OnButtonClicked(digit));
    }

    private void OnButtonClicked(int digit)
    {
        submitCallback?.Invoke(index, digit);
    }

    public void MarkSolved()
    {
        // disable all buttons and show solved overlay
        foreach (var b in buttonsParent.GetComponentsInChildren<Button>())
            b.interactable = false;

        if (solvedOverlay != null)
            solvedOverlay.gameObject.SetActive(true);
    }

    public void FlashWrong()
    {
        // simple visual feedback: pulse red on expression text
        if (expressionText != null)
        {
            var orig = expressionText.color;
            StopAllCoroutines();
            StartCoroutine(Flash(orig));
        }
    }

    private System.Collections.IEnumerator Flash(Color orig)
    {
        expressionText.color = Color.red;
        yield return new WaitForSeconds(0.25f);
        expressionText.color = orig;
    }
}