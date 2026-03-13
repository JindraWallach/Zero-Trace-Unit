using UnityEngine;
using UnityEngine.UI;

namespace ZeroTrace.UI.Suspicion
{
    public sealed class EnemySuspicionBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private SuspicionIndicatorConfig _config;

        private float _lastSuspicion = -1f;
        private Color _lastColor;

        private EnemySuspicionSystem _suspicionSystem;
        private EnemyWorldToScreenPositioner _positioner;

        public void Initialize(EnemySuspicionSystem system, EnemyWorldToScreenPositioner positioner)
        {
            if (_config == null)
                Debug.LogError("[EnemySuspicionBarView] SuspicionIndicatorConfig not assigned!", this);

            _suspicionSystem = system;
            _positioner = positioner;

            _fillImage.transform.localScale = new Vector3(_config != null ? _config.scaleDefault : 1f, 1f, 1f);
            Hide();
        }

        public void SubscribeToSystem()
        {
            _suspicionSystem.OnSuspicionChanged += OnSuspicionChanged;
            _suspicionSystem.OnSuspicionCleared += OnSuspicionCleared;
        }

        public void UnsubscribeFromSystem()
        {
            if (_suspicionSystem == null) return;
            _suspicionSystem.OnSuspicionChanged -= OnSuspicionChanged;
            _suspicionSystem.OnSuspicionCleared -= OnSuspicionCleared;
        }

        private void OnSuspicionChanged(float suspicion)
        {
            if (!_suspicionSystem.IsPlayerVisible)
            {
                Hide();
                return;
            }

            Show();
            UpdateVisual(suspicion);
        }

        private void OnSuspicionCleared() => Hide();

        private void Show() => gameObject.SetActive(true);

        private void Hide()
        {
            gameObject.SetActive(false);
            _lastSuspicion = -1f;
        }

        private void UpdateVisual(float suspicion)
        {
            float threshold = _config != null ? _config.changeThreshold : 0.5f;
            if (Mathf.Abs(suspicion - _lastSuspicion) < threshold) return;
            _lastSuspicion = suspicion;

            float t = suspicion / 100f;
            float scaleX = Mathf.Lerp(_config.scaleMin, _config.scaleMax, t);
            _fillImage.transform.localScale = new Vector3(scaleX, 1f, 1f);

            Color c = ComputeColor(t);
            if (c != _lastColor)
            {
                _lastColor = c;
                _fillImage.color = c;
            }
        }

        private Color ComputeColor(float t)
        {
            if (_config == null) return Color.yellow;
            if (t < 0.5f)
                return Color.Lerp(_config.colorLow, _config.colorMedium, t * 2f);
            return Color.Lerp(_config.colorMedium, _config.colorHigh, (t - 0.5f) * 2f);
        }

        private void OnDestroy() => UnsubscribeFromSystem();
    }
}