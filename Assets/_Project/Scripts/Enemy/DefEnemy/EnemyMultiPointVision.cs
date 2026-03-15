using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-point detection system (4 raycasts + 360° proximity fallback).
///
/// FIX 1 – Proximity Detection:
///   Před standardním FOV checkem zkontroluje vzdálenost hráče.
///   Pokud je blíže než EnemyConfig.proximityRadius, suspicion okamžitě na 100 %
///   bez ohledu na FOV, směr pohledu ani překážky. Eliminuje obcházení zezadu.
///
/// SRP:  Detekuje viditelnost. Nic víc.
/// OOP:  Závisí na EnemySuspicionSystem přes interface zápis (SetPlayerVisible / AddSuspicion).
/// Perf: Checks řízeny EnemyDetectionManagerem (coroutiny s nastavitelným intervalem).
/// </summary>
public class EnemyMultiPointVision : MonoBehaviour
{
    [Header("Manual Setup (Required)")]
    [Tooltip("Player body points – assign 4 transforms: Head, Torso, LeftHand, RightHand")]
    [SerializeField] private Transform[] playerBodyPoints = new Transform[4];

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<int> OnVisibilityChanged; // 0–4 visible points
    public event Action<Vector3> OnPlayerSpotted;
    public event Action<Vector3> OnPlayerLostSight;

    // ── Private references ───────────────────────────────────────────────────
    private Transform eyePosition;
    private Transform playerTransform;
    private EnemySuspicionSystem suspicionSystem;
    private EnemyConfig config;
    private SuspicionConfig suspicionConfig;

    // ── State ────────────────────────────────────────────────────────────────
    private int visiblePointsCount;
    private bool[] pointVisibility = new bool[4];
    private bool canSeePlayer;
    private bool wasVisible;

    private Coroutine visionCheckCoroutine;

    // ── Debug visualisation data ─────────────────────────────────────────────
    private readonly Vector3[] lastRayStarts = new Vector3[4];
    private readonly Vector3[] lastRayDirections = new Vector3[4];
    private readonly bool[] lastRaycastHits = new bool[4];
    private readonly RaycastHit[] lastHitInfo = new RaycastHit[4];
    private readonly float[] lastAngles = new float[4];
    private readonly float[] lastDistances = new float[4];

    // ── Public API ────────────────────────────────────────────────────────────
    public int VisiblePoints => visiblePointsCount;
    public bool CanSeePlayer => canSeePlayer;

    /// <summary>Called by EnemyDetectionManager – compatibility wrapper.</summary>
    public void PerformDetectionCheck() => PerformVisionCheck();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        EnemyDetectionManager.Instance?.UnregisterDetector(this);
    }

    public void Initialize(EnemyStateMachine machine, EnemyConfig enemyConfig,
                           SuspicionConfig susConfig, Transform player)
    {
        config = enemyConfig;
        suspicionConfig = susConfig;
        playerTransform = player;
        suspicionSystem = machine.GetComponent<EnemySuspicionSystem>();

        // Eye position – find child nebo vytvoř
        eyePosition = transform.Find("EyePosition");
        if (eyePosition == null)
        {
            var eyeObj = new GameObject("EyePosition");
            eyeObj.transform.SetParent(transform);
            eyeObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            eyePosition = eyeObj.transform;
        }

        if (!ValidateSetup())
        {
            Debug.LogError($"[EnemyMultiPointVision] {gameObject.name}: setup invalid – disabling.", this);
            enabled = false;
            return;
        }

        // Registrace do batch detection manageru
        EnemyDetectionManager.Instance?.RegisterDetector(this);

        if (config.debugStates)
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name} initialized.", this);
    }

    private void OnEnable()
    {
        if (config != null && suspicionConfig != null)
            StartVisionChecks();
    }

    private void OnDisable()
    {
        StopVisionChecks();
    }

    // ── Vision check loop ────────────────────────────────────────────────────

    private void StartVisionChecks()
    {
        StopVisionChecks();
        visionCheckCoroutine = StartCoroutine(VisionCheckCoroutine());
    }

    private void StopVisionChecks()
    {
        if (visionCheckCoroutine == null) return;
        StopCoroutine(visionCheckCoroutine);
        visionCheckCoroutine = null;
    }

    private IEnumerator VisionCheckCoroutine()
    {
        var wait = new WaitForSeconds(suspicionConfig.visionCheckInterval);
        while (true)
        {
            PerformVisionCheck();
            yield return wait;
        }
    }

    // ── Core detection ────────────────────────────────────────────────────────

    private void PerformVisionCheck()
    {
        if (playerTransform == null || eyePosition == null)
        {
            canSeePlayer = false;
            return;
        }

        // ── FIX 1: Proximity 360° detection ──────────────────────────────────
        // Pokud je hráč do proximityRadius, okamžitá detekce – žádný FOV, žádný raycast.
        // Brání obcházení zezadu nebo skrze malé mezery.
        if (config.proximityRadius > 0f)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= config.proximityRadius)
            {
                HandleProximityDetection();
                return;
            }
        }

        // ── Standard multi-point FOV detection ───────────────────────────────
        visiblePointsCount = 0;

        for (int i = 0; i < 4; i++)
        {
            if (playerBodyPoints[i] == null)
            {
                pointVisibility[i] = false;
                lastRaycastHits[i] = false;
                continue;
            }

            pointVisibility[i] = CheckPointVisibility(i, playerBodyPoints[i].position);
            if (pointVisibility[i]) visiblePointsCount++;
        }

        bool nowVisible = visiblePointsCount > 0;
        FireVisibilityEvents(nowVisible);

        canSeePlayer = nowVisible;
        wasVisible = nowVisible;

        suspicionSystem?.SetPlayerVisible(canSeePlayer, visiblePointsCount);
        OnVisibilityChanged?.Invoke(visiblePointsCount);

        if (config.debugStates)
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name}: {visiblePointsCount}/4 visible, CanSee={canSeePlayer}", this);
    }

    /// <summary>
    /// Okamžitá detekce z blízkosti – suspicion na 100 %, fire events.
    /// </summary>
    private void HandleProximityDetection()
    {
        // Naplň suspicion okamžitě
        suspicionSystem?.AddSuspicion(100f);

        bool wasVisibleBefore = canSeePlayer;
        canSeePlayer = true;
        visiblePointsCount = 4; // Symbolicky – "vidí všechno"

        if (!wasVisibleBefore)
            OnPlayerSpotted?.Invoke(playerTransform.position);

        wasVisible = true;
        OnVisibilityChanged?.Invoke(4);

        if (config.debugStates)
        {
            float d = Vector3.Distance(transform.position, playerTransform.position);
            Debug.Log($"[EnemyMultiPointVision] {gameObject.name}: PROXIMITY DETECTED at {d:F2}m (radius {config.proximityRadius}m)", this);
        }
    }

    /// <summary>Fires OnPlayerSpotted / OnPlayerLostSight based on state change.</summary>
    private void FireVisibilityEvents(bool nowVisible)
    {
        if (nowVisible && !wasVisible)
            OnPlayerSpotted?.Invoke(playerTransform.position);
        else if (!nowVisible && wasVisible)
            OnPlayerLostSight?.Invoke(playerTransform.position);
    }

    private bool CheckPointVisibility(int idx, Vector3 targetPoint)
    {
        Vector3 dir = (targetPoint - eyePosition.position).normalized;
        float dist = Vector3.Distance(eyePosition.position, targetPoint);

        lastRayDirections[idx] = dir;
        lastDistances[idx] = dist;

        // 1. Range
        if (dist > config.visionRange)
        {
            lastRaycastHits[idx] = false;
            return false;
        }

        // 2. FOV angle
        float angle = Vector3.Angle(transform.forward, dir);
        lastAngles[idx] = angle;
        if (angle > config.visionAngle * 0.5f)
        {
            lastRaycastHits[idx] = false;
            return false;
        }

        // 3. Obstacle raycast
        Vector3 rayStart = eyePosition.position + dir * 0.1f;
        float rayDist = dist - 0.1f;
        lastRayStarts[idx] = rayStart;

        bool hitObstacle = Physics.Raycast(rayStart, dir,
                                           out RaycastHit hit, rayDist,
                                           config.visionObstacleMask);
        lastRaycastHits[idx] = hitObstacle;

        if (hitObstacle)
        {
            lastHitInfo[idx] = hit;
            if (config.debugStates)
                Debug.Log($"[EnemyMultiPointVision] Point {idx}: BLOCKED by {hit.collider.name}", this);
            return false;
        }

        return true;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateSetup()
    {
        bool ok = true;
        if (eyePosition == null) { Debug.LogError("[MPVision] Eye position not set!"); ok = false; }
        if (playerTransform == null) { Debug.LogError("[MPVision] Player transform not set!"); ok = false; }
        if (config == null) { Debug.LogError("[MPVision] EnemyConfig not set!"); ok = false; }
        if (suspicionConfig == null) { Debug.LogError("[MPVision] SuspicionConfig not set!"); ok = false; }

        int valid = 0;
        foreach (var p in playerBodyPoints) if (p != null) valid++;
        if (valid == 0) Debug.LogError("[MPVision] No player body points assigned!");
        else if (valid < 4) Debug.LogWarning($"[MPVision] Only {valid}/4 body points assigned.");
        return ok;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos() { if (config?.debugVision == true) DrawGizmos(); }
    private void OnDrawGizmosSelected() { if (config?.debugVision == true) DrawGizmos(); }

    private void DrawGizmos()
    {
        if (eyePosition == null || config == null) return;

        float halfAngle = config.visionAngle * 0.5f;
        float range = config.visionRange;

        // Proximity sphere
        if (config.proximityRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, config.proximityRadius);
        }

        // Eye
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(eyePosition.position, 0.15f);

        // Forward
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(eyePosition.position, transform.forward * range);

        // FOV cone edges
        Vector3 fwd = transform.forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * fwd * range;
        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * fwd * range;

        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + rightEdge);
        Gizmos.DrawLine(eyePosition.position, eyePosition.position + leftEdge);

        Vector3 prev = eyePosition.position + rightEdge;
        for (int i = 1; i <= 20; i++)
        {
            float a = Mathf.Lerp(-halfAngle, halfAngle, i / 20f);
            Vector3 pt = eyePosition.position + Quaternion.Euler(0, a, 0) * fwd * range;
            Gizmos.DrawLine(prev, pt);
            prev = pt;
        }

        // Raycasts to body parts
        if (playerTransform == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (playerBodyPoints[i] == null) continue;
            Vector3 tgt = playerBodyPoints[i].position;

            Gizmos.color = pointVisibility[i] ? Color.green : new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawLine(eyePosition.position, tgt);

            Gizmos.color = pointVisibility[i] ? Color.green : Color.red;
            Gizmos.DrawWireSphere(tgt, 0.15f);

            if (lastRayStarts[i] != Vector3.zero)
            {
                if (lastRaycastHits[i])
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(lastRayStarts[i], lastHitInfo[i].point);
                    Gizmos.DrawWireSphere(lastHitInfo[i].point, 0.08f);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(lastRayStarts[i], tgt);
                }
            }
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            eyePosition.position + Vector3.up * 0.5f,
            $"Visible: {visiblePointsCount}/4  CanSee: {canSeePlayer}\n" +
            $"FOV: {config.visionAngle:F0}°  Range: {config.visionRange:F1}m\n" +
            $"Proximity: {config.proximityRadius:F1}m",
            new GUIStyle
            {
                normal = new GUIStyleState { textColor = canSeePlayer ? Color.green : Color.yellow },
                fontSize = 10,
                fontStyle = FontStyle.Bold
            }
        );
#endif
    }
}