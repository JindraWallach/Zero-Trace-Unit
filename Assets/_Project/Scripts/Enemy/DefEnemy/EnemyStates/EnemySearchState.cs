using UnityEngine;

/// <summary>
/// Search state - enemy lost player, searches last known area.
/// Moves to random points around last known position.
/// Transitions to Chase if player re-spotted, or returns to Patrol after timeout.
/// UPDATED: Uses suspicion system for detection.
/// </summary>
public class EnemySearchState : EnemyState
{
    private Vector3 searchCenter;
    private Vector3 currentSearchPoint;
    private float searchTimer;
    private float pointTimer;
    private int searchPointsChecked;
    private const float TIME_PER_SEARCH_POINT = 3f;
    private const int MAX_SEARCH_POINTS = 4;

    public EnemySearchState(EnemyStateMachine machine, Vector3 lastKnownPosition) : base(machine)
    {
        searchCenter = lastKnownPosition;
    }

    public override void Enter()
    {
        // Set search speed and alert animation
        machine.Animation.SetAlert(true);

        searchTimer = 0f;
        pointTimer = 0f;
        searchPointsChecked = 0;

        // Move to first search point
        MoveToNextSearchPoint();

        if (machine.Config.debugStates)
            Debug.Log($"[EnemySearch] {machine.gameObject.name} searching around {searchCenter}", machine);
    }

    public override void Update()
    {
        searchTimer += Time.deltaTime;
        pointTimer += Time.deltaTime;

        // Check if we found player
        if (CanSeePlayer())
        {
            // Player found - suspicion will build, let system decide chase
            if (ShouldChase())
            {
                machine.SetState(new EnemyChaseState(machine));
                return;
            }
        }

        // Check if reached current search point
        if (machine.Movement.HasReachedDestination || pointTimer >= TIME_PER_SEARCH_POINT)
        {
            searchPointsChecked++;
            pointTimer = 0f;

            // Check if searched enough points
            if (searchPointsChecked >= MAX_SEARCH_POINTS)
            {
                // Give up search - return to patrol/idle
                GiveUpSearch();
                return;
            }

            // Move to next search point
            MoveToNextSearchPoint();
        }

        // Timeout check - give up after search duration
        if (searchTimer >= machine.Config.searchDuration)
        {
            GiveUpSearch();
        }
    }

    public override void OnPlayerDetected(Vector3 playerPosition)
    {
        // Found player - suspicion system will handle transition
        searchCenter = playerPosition; // Update search center
    }

    private void MoveToNextSearchPoint()
    {
        // Get random point around search center
        currentSearchPoint = machine.Movement.GetRandomPointInRadius(searchCenter, machine.Config.searchRadius);
        machine.Movement.MoveToPosition(currentSearchPoint, machine.Config.searchSpeed);

        if (machine.Config.debugStates)
            Debug.Log($"[EnemySearch] {machine.gameObject.name} searching point {searchPointsChecked + 1}/{MAX_SEARCH_POINTS}", machine);
    }

    private void GiveUpSearch()
    {
        // Clear memory and return to normal behavior
        machine.ClearMemory();

        if (machine.PatrolRoute != null && machine.PatrolRoute.WaypointCount >= 2)
        {
            machine.SetState(new EnemyPatrolState(machine));
        }
        else
        {
            machine.SetState(new EnemyIdleState(machine));
        }

        if (machine.Config.debugStates)
            Debug.Log($"[EnemySearch] {machine.gameObject.name} gave up search, returning to patrol", machine);
    }

    public override void Exit()
    {
        machine.Animation.SetAlert(false);
    }
}