using System.Collections.Generic;
using UnityEngine;

public class DoorInteractionMode : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoorStateMachine stateMachine;
    [SerializeField] private HackableDoor hackableDoor;
    [SerializeField] private DoorController doorController;

    [Header("Config")]
    [SerializeField] private DoorInteractionConfig config;

    private readonly List<IInteractionStrategy> normalModeStrategies = new();
    private readonly List<IInteractionStrategy> hackModeStrategies = new();

    private DoorContext context;
    private IInteractionStrategy currentStrategy;

    private void Awake()
    {
        // Normal mode: only physical interaction
        normalModeStrategies.Add(new PhysicalInteractionStrategy());

        // Hack mode: hack or already hacked
        hackModeStrategies.Add(new HackInteractionStrategy());
        hackModeStrategies.Add(new AlreadyHackedStrategy());
        hackModeStrategies.Add(new OutOfRangeStrategy());

        context = new DoorContext
        {
            StateMachine = stateMachine,
            HackableDoor = hackableDoor,
            Config = config,
            IsLocked = stateMachine.Lock.IsLocked
        };

        // Subscribe to mode changes
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged += OnModeChanged;
    }

    private void OnDestroy()
    {
        if (PlayerModeController.Instance != null)
            PlayerModeController.Instance.OnModeChanged -= OnModeChanged;
    }

    public void SetPlayerInRange(Transform player, bool inRange)
    {
        context.Player = player;

        if (!inRange)
        {
            doorController.HidePrompts();
            currentStrategy = null;
            return;
        }

        UpdateInteraction();
    }

    private void OnModeChanged(PlayerMode mode)
    {
        if (context.Player != null)
            UpdateInteraction();
    }

    private void UpdateInteraction()
    {
        if (context.Player == null) return;

        context.Distance = Vector3.Distance(transform.position, context.Player.position);
        context.IsLocked = stateMachine.Lock.IsLocked;

        var strategies = PlayerModeController.Instance.CurrentMode == PlayerMode.Hack
            ? hackModeStrategies
            : normalModeStrategies;

        currentStrategy = FindFirstValidStrategy(strategies);

        if (currentStrategy != null)
            doorController.SetPromptEnabled(true, currentStrategy.GetPromptText(context));
        else
            doorController.HidePrompts();
    }

    private IInteractionStrategy FindFirstValidStrategy(List<IInteractionStrategy> strategies)
    {
        foreach (var strategy in strategies)
        {
            if (strategy.CanExecute(context))
                return strategy;
        }
        return null;
    }

    public void ExecuteInteraction()
    {
        currentStrategy?.Execute(context);
    }
}