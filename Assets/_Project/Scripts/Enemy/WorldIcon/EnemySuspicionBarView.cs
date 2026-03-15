using UnityEngine;
using UnityEngine.UI;

namespace ZeroTrace.UI.Suspicion
{
    public sealed class EnemySuspicionBarView : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private SuspicionIndicatorConfig _config;

        private RectTransform _rt;
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
            _rt = GetComponent<RectTransform>();

            // Nastav výchozí velikost GO, image se nedotýkáme
            if (_config != null)
                _rt.sizeDelta = _config.defaultSize;

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
            if (Mathf.Abs(suspicion - _lastSuspicion) < _config.changeThreshold) return;
            _lastSuspicion = suspicion;

            // Škáluj celý GO od 1 do scaleMax podle suspicion
            float scale = Mathf.Lerp(1f, _config.scaleMax, suspicion / 100f);
            transform.localScale = Vector3.one * scale;

            Color c = ComputeColor(suspicion / 100f);
            if (c != _lastColor)
            {
                _lastColor = c;
                _fillImage.color = c;
            }
        }

        private Color ComputeColor(float t)
        {
            if (t < 0.5f)
                return Color.Lerp(_config.colorLow, _config.colorMedium, t * 2f);
            return Color.Lerp(_config.colorMedium, _config.colorHigh, (t - 0.5f) * 2f);
        }

        private void OnDestroy() => UnsubscribeFromSystem();
    }
}