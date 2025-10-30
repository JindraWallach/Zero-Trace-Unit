using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for a single math row (expression + digit buttons).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MathRowUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text expressionText;
    [SerializeField] private Transform buttonsParent;
    [SerializeField] private Image solvedOverlay;

    private int index;
    private Action<int, int> submitCallback;

    public void Setup(int index, string expression, GameObject digitButtonPrefab, Action<int, int> submitCallback)
    {
        this.index = index;
        this.submitCallback = submitCallback;

        if (expressionText != null)
            expressionText.text = expression;

        CreateDigitButtons(digitButtonPrefab);

        if (solvedOverlay != null)
            solvedOverlay.gameObject.SetActive(false);
    }

    private void CreateDigitButtons(GameObject prefab)
    {
        for (int d = 1; d <= 9; d++)
            CreateButton(d, prefab);

        CreateButton(0, prefab); // 0 last
    }

    private void CreateButton(int digit, GameObject prefab)
    {
        var go = Instantiate(prefab, buttonsParent);
        var btn = go.GetComponent<Button>();
        var label = go.GetComponentInChildren<TMP_Text>();

        if (label != null)
            label.text = digit.ToString();

        btn.onClick.AddListener(() => OnButtonClicked(digit));
    }

    private void OnButtonClicked(int digit)
    {
        submitCallback?.Invoke(index, digit);
    }

    public void MarkSolved()
    {
        foreach (var btn in buttonsParent.GetComponentsInChildren<Button>())
            btn.interactable = false;

        if (solvedOverlay != null)
            solvedOverlay.gameObject.SetActive(true);
    }
}