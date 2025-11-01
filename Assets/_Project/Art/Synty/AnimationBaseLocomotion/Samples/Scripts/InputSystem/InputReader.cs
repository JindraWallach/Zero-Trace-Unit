// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// Sample scripts are included only as examples and are not intended as production-ready.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Synty.AnimationBaseLocomotion.Samples.InputSystem
{
    public class InputReader : MonoBehaviour, Controls.IPlayerActions
    {
        public Vector2 _mouseDelta;
        public Vector2 _moveComposite;

        public float _movementInputDuration;
        public bool _movementInputDetected;

        private Controls _controls;

        // Per-action access
        private readonly Dictionary<string, InputAction> actions = new Dictionary<string, InputAction>(StringComparer.OrdinalIgnoreCase);

        // Optional override; if empty, we will read names from the map at runtime
        [SerializeField]
        private string[] actionNames = Array.Empty<string>();

        public Action onAimActivated;
        public Action onAimDeactivated;

        public Action onCrouchActivated;
        public Action onCrouchDeactivated;

        public Action onJumpPerformed;

        public Action onLockOnToggled;

        public Action onSprintActivated;
        public Action onSprintDeactivated;

        public Action onWalkToggled;

        public Action onFlashlightToggled;

        public Action onInteract;

        public Action onEscapePressed;

        public Action onHackModeToggle;

        /// <inheritdoc cref="OnEnable" />
        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }

            // ensure action dictionary is populated
            PopulateActionDictionary();

            // enable player map
            _controls.Player.Enable();
        }

        /// <inheritdoc cref="OnDisable" />
        public void OnDisable()
        {
            _controls.Player.Disable();
        }

        // Call this from the Inspector context menu or at runtime to refresh actionNames from the Player map
        [ContextMenu("Refresh Action Names from Player Map")]
        private void RefreshActionNames()
        {
            if (_controls == null) _controls = new Controls();
            var map = _controls.Player.Get();
            if (map == null)
            {
                actionNames = Array.Empty<string>();
                return;
            }

            actionNames = map.actions
                             .Where(a => a != null)
                             .Select(a => a.name)
                             .ToArray();

            // keep dictionary in sync
            PopulateActionDictionary();
        }

        // Expose current resolved names for other systems (optional)
        public IReadOnlyList<string> GetResolvedActionNames()
            => actionNames ?? Array.Empty<string>();

        // Build dictionary by looking up actions in the generated Player action map.
        // This lets the launcher selectively Enable/Disable individual InputAction instances
        // while still using SetCallbacks(this) to receive callbacks.
        private void PopulateActionDictionary()
        {
            actions.Clear();

            try
            {
                var map = _controls.Player.Get();
                if (map == null) return;

                if (actionNames != null && actionNames.Length > 0)
                {
                    foreach (var name in actionNames.Where(n => !string.IsNullOrWhiteSpace(n)))
                    {
                        var a = map.FindAction(name, false);
                        if (a != null && !actions.ContainsKey(name))
                            actions[name] = a;
                    }
                }
                else
                {
                    foreach (var a in map.actions)
                    {
                        if (a == null) continue;
                        if (!actions.ContainsKey(a.name))
                            actions[a.name] = a;
                    }
                }
            }
            catch (Exception)
            {
                // safe no-op
                Debug.LogWarning("InputReader: Failed to populate action dictionary.");
            }
        }

        // Use: inputReader.DisableInputs(new[] { "Exit" });
        public void DisableInputs(IEnumerable<string> excludedActions = null)
        {
            if (actions.Count == 0) PopulateActionDictionary();

            var excluded = excludedActions == null
                ? Array.Empty<string>()
                : excludedActions.Select(e => e ?? string.Empty).ToArray();

            foreach (var kv in actions)
            {
                if (excluded.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                    continue;

                try { kv.Value.Disable(); }
                catch { /* ignore invalid state */ }
            }
        }

        public void EnableInputs(IEnumerable<string> onlyThese = null)
        {
            if (actions.Count == 0) PopulateActionDictionary();

            if (onlyThese == null)
            {
                foreach (var a in actions.Values)
                {
                    try { a.Enable(); } catch { }
                }

                return;
            }

            var only = onlyThese.Select(e => e ?? string.Empty).ToArray();
            foreach (var kv in actions)
            {
                if (only.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                {
                    try { kv.Value.Enable(); } catch { }
                }
                else
                {
                    try { kv.Value.Disable(); } catch { }
                }
            }
        }

        public void DisableAllExcept(IEnumerable<string> allowed)
        {
            if (actions.Count == 0) PopulateActionDictionary();
            var allow = allowed?.Select(a => a ?? string.Empty).ToArray() ?? Array.Empty<string>();

            foreach (var kv in actions)
            {
                if (allow.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                {
                    try { kv.Value.Enable(); } catch { }
                }
                else
                {
                    try { kv.Value.Disable(); } catch { }
                }
            }
        }

        public void EnableAllInputs()
        {
            if (actions.Count == 0) PopulateActionDictionary();
            foreach (var a in actions.Values)
            {
                try { a.Enable(); } catch { }
            }
        }

        // Existing callback implementations (unchanged logic)
        public void OnLook(InputAction.CallbackContext context)
        {
            _mouseDelta = context.ReadValue<Vector2>();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveComposite = context.ReadValue<Vector2>();
            _movementInputDetected = _moveComposite.magnitude > 0;
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onJumpPerformed?.Invoke();
        }

        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onWalkToggled?.Invoke();
        }

        public void OnSprint(InputAction.CallbackContext context)
        {
            if (context.started) onSprintActivated?.Invoke();
            else if (context.canceled) onSprintDeactivated?.Invoke();
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            if (context.started) onCrouchActivated?.Invoke();
            else if (context.canceled) onCrouchDeactivated?.Invoke();
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            if (context.started) onAimActivated?.Invoke();
            if (context.canceled) onAimDeactivated?.Invoke();
        }

        public void OnLockOn(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onLockOnToggled?.Invoke();
            onSprintDeactivated?.Invoke();
        }

        public void OnToggleFlashlight(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onFlashlightToggled?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onInteract?.Invoke();
        }

        public void OnExit(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onEscapePressed?.Invoke();
        }

        public void OnChangeMode(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            onHackModeToggle?.Invoke();
        }
    }
}
