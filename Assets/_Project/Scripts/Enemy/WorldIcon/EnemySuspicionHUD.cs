using System.Collections.Generic;
using UnityEngine;

namespace ZeroTrace.UI.Suspicion
{
    /// <summary>
    /// Vyžaduje patch do EnemyStateMachine — viz PATCHES.cs:
    ///   public event Action<EnemyStateMachine> OnEnemyDestroyed;
    ///   v OnDestroy(): OnEnemyDestroyed?.Invoke(this);
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class EnemySuspicionHUD : MonoBehaviour
    {
        [SerializeField] private EnemySuspicionBarView _indicatorPrefab;
        [SerializeField] private SuspicionIndicatorConfig _config;
        [SerializeField] private Camera _overrideCamera;

        private Canvas _canvas;
        private Camera _camera;

        private readonly Dictionary<EnemyStateMachine, EnemySuspicionBarView> _indicators
            = new Dictionary<EnemyStateMachine, EnemySuspicionBarView>();

        private readonly List<EnemyStateMachine> _pendingRemoval = new List<EnemyStateMachine>();

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = _overrideCamera != null ? _overrideCamera : Camera.main;

            if (_config == null)
                Debug.LogError("[EnemySuspicionHUD] SuspicionIndicatorConfig not assigned!", this);
            if (_camera == null)
                Debug.LogError("[EnemySuspicionHUD] No camera found.", this);
        }

        private void Start()
        {
            EnemyStateMachine[] enemies =
                FindObjectsByType<EnemyStateMachine>(FindObjectsSortMode.None);

            foreach (EnemyStateMachine e in enemies)
                RegisterEnemy(e);

            Debug.Log($"[EnemySuspicionHUD] Registered {enemies.Length} enemies.", this);
        }

        private void LateUpdate()
        {
            if (_pendingRemoval.Count == 0) return;

            foreach (EnemyStateMachine dead in _pendingRemoval)
                RemoveIndicator(dead);

            _pendingRemoval.Clear();
        }

        private void OnDestroy()
        {
            foreach (var kvp in _indicators)
            {
                if (kvp.Key != null)
                    kvp.Key.OnEnemyDestroyed -= OnEnemyDestroyed;
                if (kvp.Value != null)
                    kvp.Value.UnsubscribeFromSystem();
            }
            _indicators.Clear();
        }

        public void RegisterEnemy(EnemyStateMachine enemy)
        {
            if (enemy == null || _indicators.ContainsKey(enemy)) return;

            EnemySuspicionSystem sys = enemy.Suspicion;
            if (sys == null)
            {
                Debug.LogWarning($"[EnemySuspicionHUD] {enemy.name} has no SuspicionSystem.", this);
                return;
            }

            EnemySuspicionBarView view = Instantiate(_indicatorPrefab, transform);

            RectTransform rt = view.GetComponent<RectTransform>();
            rt.sizeDelta = _config != null ? _config.indicatorSize : new Vector2(80f, 12f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);

            EnemyWorldToScreenPositioner positioner =
                view.GetComponent<EnemyWorldToScreenPositioner>();

            positioner.Initialize(enemy.transform, _camera, _canvas);
            view.Initialize(sys, positioner);
            view.SubscribeToSystem();

            _indicators[enemy] = view;
            enemy.OnEnemyDestroyed += OnEnemyDestroyed;
        }

        private void OnEnemyDestroyed(EnemyStateMachine enemy) =>
            _pendingRemoval.Add(enemy);

        private void RemoveIndicator(EnemyStateMachine enemy)
        {
            if (!_indicators.TryGetValue(enemy, out EnemySuspicionBarView view)) return;

            _indicators.Remove(enemy);
            enemy.OnEnemyDestroyed -= OnEnemyDestroyed;

            if (view != null)
            {
                view.UnsubscribeFromSystem();
                Destroy(view.gameObject);
            }
        }
    }
}