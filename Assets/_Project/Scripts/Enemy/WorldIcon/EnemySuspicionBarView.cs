using UnityEngine;
using UnityEngine.UI;

namespace ZeroTrace.UI.Suspicion
{
    public sealed class EnemySuspicionBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private SuspicionIndicatorConfig _config;

        private float _lastFill = -1f;
        private Color _lastColor;

        private EnemySuspicionSystem _suspicionSystem;
        private EnemyWorldToScreenPositioner _positioner;

        public void Initialize(EnemySuspicionSystem system, EnemyWorldToScreenPositioner positioner)
        {
            if (_config == null)
                Debug.LogError("[EnemySuspicionBarView] SuspicionIndicatorConfig not assigned!", this);

            _suspicionSystem = system;
            _positioner = positioner;
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
            // Zobraz kdykoli enemy vidí hráče — i při suspicion = 0 na začátku
            if (!_suspicionSystem.IsPlayerVisible)
            {
                Hide();
                return;
            }

            Show();
            SetFill(suspicion * 0.01f);
        }

        private void OnSuspicionCleared() => Hide();

        private void Show() => gameObject.SetActive(true);

        private void Hide()
        {
            gameObject.SetActive(false);
            _lastFill = -1f;
        }

        private void SetFill(float t)
        {
            float threshold = _config != null ? _config.fillChangeThreshold : 0.005f;
            if (Mathf.Abs(t - _lastFill) < threshold) return;
            _lastFill = t;

            Color c = ComputeColor(t);
            if (c != _lastColor)
            {
                _lastColor = c;
                _fillImage.color = c;
            }
            _fillImage.fillAmount = t;
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