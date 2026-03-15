using UnityEngine;

/// <summary>
/// Chase state – enemy aktivně pronásleduje hráče.
///
/// FIX 2 – Fast Detection Interval:
///   Enter() → SetFastInterval (0.05s) pro okamžitou detekci během sprintu/skoku.
///   Exit()  → RestoreInterval zpět na normální hodnotu z SuspicionConfig.
///   Zabraňuje "projití mezi checki" při rychlém pohybu hráče.
///
/// FIX 3 – Path Blocked → Alarm:
///   Pokud je cesta zablokovaná ≥ BLOCKED_ALARM_TIMEOUT sekund:
///     - Enemy VIDÍ hráče:  spustí alarm (hráč je ve viditelné díře/boxu).
///     - Enemy NEVIDÍ hráče: přejde do SearchState (původní chování).
///   Enemy v obou případech čelí hráči a neztrácí ho ze zřetele.
///
/// SRP:  Logika pronásledování. Deleguje alarm na SecurityAlarmSystem.
/// OOP:  Přistupuje k systémům přes EnemyStateMachine gettery.
/// </summary>
public class EnemyChaseState : EnemyState
{
    // ── Konstanty ─────────────────────────────────────────────────────────────
    private const float LOSE_PLAYER_DELAY = 2f;   // sekund bez vizuálního kontaktu → Search
    private const float PATH_CHECK_INTERVAL = 0.5f; // jak často kontrolovat NavMesh path
    private const float BLOCKED_PATROL_TIMEOUT = 5f;   // sekund blocked bez LOS → Patrol
    private const float BLOCKED_ALARM_TIMEOUT = 3f;   // sekund blocked S LOS → Alarm

    // ── State ─────────────────────────────────────────────────────────────────
    private float chaseTimer;
    private float lastSeenTimer;
    private float blockedTimer;
    private float pathCheckTimer;
    private bool alarmTriggered; // alarm spuštěn jen jednou per chase

    public EnemyChaseState(EnemyStateMachine machine) : base(machine) { }

    // ── Enter / Exit ──────────────────────────────────────────────────────────

    public override void Enter()
    {
        machine.Animation.SetAlert(true);

        chaseTimer = 0f;
        lastSeenTimer = 0f;
        blockedTimer = 0f;
        pathCheckTimer = 0f;
        alarmTriggered = false;

        // FIX 2: přepni na rychlou detekci
        EnemyDetectionManager.Instance?.SetFastInterval(machine.MultiPointVision);

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyChase] {machine.gameObject.name}: Enter (fast detection ON)", machine);
    }

    public override void Exit()
    {
        machine.Movement.Stop();

        // FIX 2: obnov normální detekci
        EnemyDetectionManager.Instance?.RestoreInterval(machine.MultiPointVision);

        if (machine.Config.debugStates)
            Debug.Log($"[EnemyChase] {machine.gameObject.name}: Exit (fast detection OFF)", machine);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    public override void Update()
    {
        chaseTimer += Time.deltaTime;
        pathCheckTimer += Time.deltaTime;

        // ── FIX 3: Path blocking check (každých PATH_CHECK_INTERVAL sekund) ──
        if (pathCheckTimer >= PATH_CHECK_INTERVAL)
        {
            pathCheckTimer = 0f;
            HandlePathBlocking();
        }

        // ── Visibility logic ──────────────────────────────────────────────────
        bool playerVisible = CanSeePlayer();

        if (!playerVisible)
        {
            lastSeenTimer += Time.deltaTime;

            // Přesuň se na poslední známou pozici
            if (machine.HasSeenPlayer)
                machine.Movement.MoveToPosition(machine.LastKnownPlayerPosition, machine.Config.chaseSpeed);

            // Příliš dlouho bez vizuálu → Search
            if (lastSeenTimer >= LOSE_PLAYER_DELAY)
            {
                machine.SetState(new EnemySearchState(machine, machine.LastKnownPlayerPosition));
                return;
            }

            return;
        }

        // ── Hráč je viditelný ─────────────────────────────────────────────────
        lastSeenTimer = 0f;

        if (machine.PlayerTransform == null) return;

        float distance = GetDistanceToPlayer();

        // Catch range – chyť hráče
        if (distance <= machine.Config.catchRange)
        {
            machine.SetState(new EnemyCatchState(machine));
            return;
        }

        // Pronásleduj
        machine.Movement.ChaseTarget(machine.PlayerTransform, machine.Config.chaseSpeed);
    }

    // ── FIX 3: Path blocking logic ────────────────────────────────────────────

    /// <summary>
    /// Zkontroluje stav NavMesh cesty a reaguje podle toho, zda enemy hráče vidí.
    /// </summary>
    private void HandlePathBlocking()
    {
        if (machine.Movement.IsPathBlocked())
        {
            blockedTimer += PATH_CHECK_INTERVAL;

            if (CanSeePlayer())
            {
                // Hráč je ve viditelné, ale nedosažitelné pozici (díra, box apod.)
                // → čelíme hráči a po timeoutu spustíme alarm
                machine.Movement.FacePosition(GetPlayerPosition());

                if (!alarmTriggered && blockedTimer >= BLOCKED_ALARM_TIMEOUT)
                {
                    TriggerBlockedAlarm();
                }
            }
            else
            {
                // Zablokováno a hráč není vidět → vrátíme se do patrolu
                if (blockedTimer >= BLOCKED_PATROL_TIMEOUT)
                {
                    if (machine.Config.debugStates)
                        Debug.Log($"[EnemyChase] {machine.gameObject.name}: path blocked {BLOCKED_PATROL_TIMEOUT}s (no LOS) → Patrol", machine);

                    machine.SetState(new EnemyPatrolState(machine));
                }
            }
        }
        else
        {
            // Cesta volná – resetuj timer
            blockedTimer = 0f;
        }
    }

    /// <summary>
    /// Spustí alarm přes SecurityAlarmSystem (jednou za chase session).
    /// </summary>
    private void TriggerBlockedAlarm()
    {
        alarmTriggered = true;

        SecurityAlarmSystem alarm = machine.AlarmSystem;

        if (alarm != null)
        {
            alarm.TriggerAlarm(machine.transform.position);

            if (machine.Config.debugStates)
                Debug.Log($"[EnemyChase] {machine.gameObject.name}: path blocked {BLOCKED_ALARM_TIMEOUT}s " +
                          $"with LOS → ALARM triggered!", machine);
        }
        else
        {
            Debug.LogWarning($"[EnemyChase] {machine.gameObject.name}: SecurityAlarmSystem not found – alarm not triggered.", machine);
        }
    }

    // ── Event overrides ───────────────────────────────────────────────────────

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        lastSeenTimer = 0f;
    }

    public override void OnPlayerLost(Vector3 lastKnownPosition)
    {
        lastSeenTimer = 0f;
    }
}