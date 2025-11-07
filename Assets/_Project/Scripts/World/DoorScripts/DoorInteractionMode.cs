using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strategy pattern controller for door interactions.
/// Updates interaction state on mode change and lock state change.
/// Event-driven architecture.
/// </summary>
public class DoorInteractionMode : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorStateMachine stateMachine;
    [SerializeField] private HackableDoor hackableDoor;
    [SerializeField] private DoorController doorController;
    [SerializeField] private Transform pivot;

    [Header("Config")]
    [SerializeField] private DoorInteractionConfig config;

    private readonly List<IInteractionStrategy> normalModeStrategies = new();
    private readonly List<IInteractionStrategy> hackModeStrategies = new();

    private DoorContext context;
    private IInteractionStrategy currentStrategy;
    private bool isPlayerInRange;

    private void Awake()
    {
        // Normal mode strategies (in priority order)
        normalModeStrategies.Add(new PhysicalInteractionStrategy());
        normalModeStrategies.Add(new NormalModeLockedStrategy());

        // Hack mode strategies (in priority order)
        hackModeStrategies.Add(new HackInteractionStrategy());
        hackModeStrategies.Add(new HackModeUnlockedStrategy());
        hackModeStrategies.Add(new OutOfRangeStrategy());

        context = new DoorContext
        {
            StateMachine = stateMachine,
            HackableDoor = hackableDoor,
            Config = config,
            IsLocked = stateMachine.Lock.IsLocked
        };

        Debug.Log($"[DoorInteractionMode] Initialized with {normalModeStrategies.Count} normal strategies, {hackModeStrategies.Count} hack strategies");
    }

    private void OnEnable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;

        // Subscribe to lock state changes
        if (stateMachine != null && stateMachine.Lock != null)
            stateMachine.Lock.OnLockStateChanged += OnLockStateChanged;
    }

    private void OnDisable()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;

        if (stateMachine != null && stateMachine.Lock != null)
            stateMachine.Lock.OnLockStateChanged -= OnLockStateChanged;
    }

    public void SetPlayerInRange(Transform player, bool inRange)
    {
        context.Player = player;
        isPlayerInRange = inRange;

        if (!inRange)
        {
            doorController.SetPlayerInRange(null, false);
            currentStrategy = null;
            Debug.Log("[DoorInteractionMode] Player left range");
            return;
        }

        doorController.SetPlayerInRange(player, true);
        Debug.Log("[DoorInteractionMode] Player entered range");
        UpdateInteraction();
    }

    private void OnModeChanged(PlayerMode mode)
    {
        // Update interaction when mode changes ONLY if player is in range
        if (isPlayerInRange && context.Player != null)
        {
            Debug.Log($"[DoorInteractionMode] Mode changed to {mode}, updating interaction");
            UpdateInteraction();
        }
    }

    private void OnLockStateChanged(bool isLocked)
    {
        // Update interaction when lock state changes ONLY if player is in range
        if (isPlayerInRange && context.Player != null)
        {
            Debug.Log($"[DoorInteractionMode] Lock state changed to {isLocked}, updating interaction");
            UpdateInteraction();
        }
    }

    private void UpdateInteraction()
    {
        if (context.Player == null) return;

        // Update context
        context.Distance = Vector3.Distance(pivot.position, context.Player.position);
        context.IsLocked = stateMachine.Lock.IsLocked;
        context.CurrentMode = PlayerModeController.Instance.CurrentMode;

        Debug.Log($"[DoorInteractionMode] UPDATE - Mode={context.CurrentMode}, Dist={context.Distance:F2}m, Locked={context.IsLocked}, PhysRange={config.physicalInteractionRange}m, HackRange={config.hackRange}m");

        // Select strategies based on current mode
        var strategies = context.CurrentMode == PlayerMode.Hack
            ? hackModeStrategies
            : normalModeStrategies;

        currentStrategy = FindFirstValidStrategy(strategies);

        if (currentStrategy != null)
        {
            string promptText = currentStrategy.GetPromptText(context);
            bool canInteract = currentStrategy.CanInteract(context);
            doorController.SetPromptEnabled(canInteract, promptText);
            Debug.Log($"[DoorInteractionMode] Selected: {currentStrategy.GetType().Name}, Prompt='{promptText}', CanInteract={canInteract}");
        }
        else
        {
            doorController.SetPromptEnabled(false);
            Debug.LogWarning("[DoorInteractionMode] No valid strategy found!");
        }
    }

    private IInteractionStrategy FindFirstValidStrategy(List<IInteractionStrategy> strategies)
    {
        Debug.Log($"[DoorInteractionMode] Testing {strategies.Count} strategies:");

        foreach (var strategy in strategies)
        {
            bool canExecute = strategy.CanExecute(context);
            Debug.Log($"[DoorInteractionMode]   - {strategy.GetType().Name}: {(canExecute ? "CAN" : "CANNOT")} execute");

            if (canExecute)
                return strategy;
        }

        return null;
    }

    public void ExecuteInteraction()
    {
        if (currentStrategy != null && currentStrategy.CanInteract(context))
        {
            Debug.Log($"[DoorInteractionMode] Executing: {currentStrategy.GetType().Name}");
            currentStrategy.Execute(context);
        }
        else
        {
            Debug.LogWarning("[DoorInteractionMode] Cannot interact - strategy doesn't allow interaction");
        }
    }
}