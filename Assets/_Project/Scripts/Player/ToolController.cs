using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages hack tool visualization and target scanning.
/// Shows lines to hackable objects in hack mode.
/// </summary>
public class ToolController : MonoBehaviour
{
    [Header("Tool Visual")]
    [SerializeField] private GameObject toolModel;
    [SerializeField] private Transform toolTip;

    [Header("Scan Settings")]
    [SerializeField] private float scanRadius = 15f;
    [SerializeField] private LayerMask hackableLayer;

    private readonly List<IHackTarget> scannedTargets = new();
    private bool isScanning;

    private void Update()
    {
        if (isScanning)
            ScanForTargets();
    }

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
        isScanning = true;
        Debug.Log("[ToolController] Scan started");
    }

    public void StopScan()
    {
        isScanning = false;
        scannedTargets.Clear();
        Debug.Log("[ToolController] Scan stopped");
    }

    private void ScanForTargets()
    {
        scannedTargets.Clear();

        var colliders = Physics.OverlapSphere(transform.position, scanRadius, hackableLayer);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out IHackTarget target))
            {
                if (target.IsHackable)
                    scannedTargets.Add(target);
            }
        }

        // UI update handled by HackOverlayUI via HackManager
    }

    public List<IHackTarget> GetScannedTargets() => scannedTargets;
}