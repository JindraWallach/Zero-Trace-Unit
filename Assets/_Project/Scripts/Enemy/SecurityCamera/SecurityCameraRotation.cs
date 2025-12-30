using System.Collections;
using UnityEngine;

/// <summary>
/// ELITE security camera rotation - ZERO Update() calls.
/// Event-driven architecture: reacts to SecurityCamera state changes.
/// Performance: Coroutines only when needed, batch operations.
/// </summary>
public class SecurityCameraRotation : MonoBehaviour, IInitializable
{
    [Header("Configuration")]
    [SerializeField] private SecurityCameraRotationConfig config;

    [Header("References")]
    [SerializeField] private Transform cameraHead;
    [SerializeField] private SecurityCamera securityCamera;
    [SerializeField] private GameObject laserPoint;
    private Transform player;

    private Coroutine activeCoroutine;

    // Track current behavior to avoid repeatedly stopping/starting coroutines every event call
    private bool isTracking;

    // Pooled objects to avoid allocations
    private readonly RaycastHit[] raycastHits = new RaycastHit[1];
    private WaitForSeconds sweepPauseWait;

    private void Awake()
    {
        // Validate config
        if (config == null)
        {
            Debug.LogError($"[SecurityCameraRotation] {name} missing SecurityCameraRotationConfig!", this);
            enabled = false;
            return;
        }

        // Cache everything once
        if (cameraHead == null) cameraHead = transform;
        if (securityCamera == null) securityCamera = GetComponent<SecurityCamera>();
        if (laserPoint != null) laserPoint.SetActive(false);

        // NOTE: player reference is provided via DependencyInjector.Initialize(...) — no tag-based search here.

        // Pre-allocate WaitForSeconds (avoids GC)
        sweepPauseWait = new WaitForSeconds(config.pauseAtEnd);
    }

    private void OnEnable()
    {
        // Subscribe to state changes (EVENT-DRIVEN, not Update)
        if (securityCamera != null)
            securityCamera.OnSuspicionChanged += OnSuspicionChanged;

        // Start initial behavior
        StartSweeping();
    }

    private void OnDisable()
    {
        // Unsubscribe
        if (securityCamera != null)
            securityCamera.OnSuspicionChanged -= OnSuspicionChanged;

        StopAllCoroutines();
        isTracking = false;
        activeCoroutine = null;

        if (laserPoint != null) laserPoint.SetActive(false);
    }

    // Called by DependencyInjector to inject dependencies (player transform, etc.)
    public void Initialize(DependencyInjector dependencyInjector)
    {
        if (dependencyInjector == null) return;
        player = dependencyInjector.PlayerPosition;
    }

    // === EVENT-DRIVEN STATE CHANGES ===

    private void OnSuspicionChanged(float suspicion)
    {
        if (suspicion > 0f && !isTracking)
        {
            // Entered suspicious state - start tracking
            StopAllCoroutines();
            isTracking = true;
            activeCoroutine = StartCoroutine(TrackingCoroutine());
        }
        else if (suspicion <= 0f && isTracking)
        {
            // Back to idle - resume sweep
            StopAllCoroutines();
            isTracking = false;
            activeCoroutine = null;
            StartSweeping();
            if (laserPoint != null) laserPoint.SetActive(false);
        }
    }

    // === SWEEP COROUTINE (Idle) ===

    private void StartSweeping()
    {
        StopAllCoroutines();
        isTracking = false;
        activeCoroutine = StartCoroutine(SweepCoroutine());
    }

    private IEnumerator SweepCoroutine()
    {
        float targetAngle = config.sweepAngleRight;
        bool movingRight = true;

        while (true)
        {
            float currentAngle = NormalizeAngle(cameraHead.localEulerAngles.y);

            // Rotate towards target
            while (Mathf.Abs(currentAngle - targetAngle) > 0.1f)
            {
                currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, config.sweepSpeed * Time.deltaTime);
                cameraHead.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
                yield return null;
            }

            // Pause at end (reuse pre-allocated WaitForSeconds)
            yield return sweepPauseWait;

            // Switch direction
            movingRight = !movingRight;
            targetAngle = movingRight ? config.sweepAngleRight : config.sweepAngleLeft;
        }
    }

    // === TRACKING COROUTINE (Suspicious/Alert) ===

    private IEnumerator TrackingCoroutine()
    {
        if (player == null) yield break;

        Debug.Log("[SecurityCameraRotation] Starting to track player.");

        // Enable laser
        if (laserPoint != null) laserPoint.SetActive(true);

        Debug.Log("[SecurityCameraRotation] Tracking player.");

        // Cache to avoid repeated calculations
        Vector3 dirToPlayer;
        Quaternion targetRot;

        while (true)
        {
            // Calculate direction (Y-axis only, no vertical tracking)
            dirToPlayer = player.position - cameraHead.position;
            dirToPlayer.y = 0f;

            if (dirToPlayer.sqrMagnitude > 0.01f)
            {
                targetRot = Quaternion.LookRotation(dirToPlayer);
                cameraHead.rotation = Quaternion.RotateTowards(
                    cameraHead.rotation,
                    targetRot,
                    config.trackingSpeed * Time.deltaTime
                );
            }

            // Update laser (non-allocating raycast)
            UpdateLaserPoint();

            yield return null;
        }
    }

    // === LASER (Non-allocating raycast) ===

    private void UpdateLaserPoint()
    {
        if (laserPoint == null) return;

        // Non-allocating raycast (uses pooled array)
        int hitCount = Physics.RaycastNonAlloc(
            cameraHead.position,
            cameraHead.forward,
            raycastHits,
            config.laserMaxDistance,
            config.laserHitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount > 0)
        {
            RaycastHit hit = raycastHits[0];
            laserPoint.transform.SetPositionAndRotation(
                hit.point + hit.normal * config.laserSurfaceOffset,
                Quaternion.LookRotation(hit.normal)
            );
        }
        else
        {
            laserPoint.SetActive(false);
        }
    }

    // === HELPERS ===

    private float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    // === DEBUG GIZMOS (Editor only) ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (config == null || !config.debugGizmos || cameraHead == null) return;

        Vector3 origin = cameraHead.position;
        float range = 10f;

        // Sweep range
        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, config.sweepAngleLeft, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, config.sweepAngleRight, 0) * transform.forward;
        Gizmos.DrawLine(origin, origin + leftDir * range);
        Gizmos.DrawLine(origin, origin + rightDir * range);

        // Current direction
        Gizmos.color = Application.isPlaying && activeCoroutine != null ? Color.red : Color.green;
        Gizmos.DrawLine(origin, origin + cameraHead.forward * range);
    }
#endif
}