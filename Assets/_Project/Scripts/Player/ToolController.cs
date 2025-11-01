// Scripts/Player/ToolController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolController : MonoBehaviour
{
    [Header("Tool Visual")]
    [SerializeField] private GameObject toolModel;
    [SerializeField] private Transform toolTip;

    [Header("Scan Settings")]
    [SerializeField] private float scanRadius = 15f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float scanInterval = 0.5f; // Scan every 0.5s instead of Update

    private readonly List<IHackTarget> scannedTargets = new();
    private Coroutine scanCoroutine;

    public void ShowTool()
    {
        if (toolModel != null)
            toolModel.SetActive(true);
    }

    public void HideTool()
    {
        if (toolModel != null)
            toolModel.SetActive(false);
    }

    public void StartScan()
    {
        StopScan();
        scanCoroutine = StartCoroutine(ScanCoroutine());
    }

    public void StopScan()
    {
        if (scanCoroutine != null)
        {
            StopCoroutine(scanCoroutine);
            scanCoroutine = null;
        }
        scannedTargets.Clear();
    }

    private IEnumerator ScanCoroutine()
    {
        var wait = new WaitForSeconds(scanInterval);

        while (true)
        {
            PerformScan();
            yield return wait;
        }
    }

    private void PerformScan()
    {
        scannedTargets.Clear();

        var colliders = Physics.OverlapSphere(transform.position, scanRadius, targetLayer);

        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IHackTarget target) && target.IsHackable)
                scannedTargets.Add(target);
        }
    }

    public List<IHackTarget> GetScannedTargets() => scannedTargets;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}