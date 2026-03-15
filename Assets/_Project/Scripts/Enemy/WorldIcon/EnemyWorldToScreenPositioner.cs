using UnityEngine;

namespace ZeroTrace.UI.Suspicion
{
    public sealed class EnemyWorldToScreenPositioner : MonoBehaviour
    {
        [SerializeField] private SuspicionIndicatorConfig _config;

        private Transform _enemyTransform;
        private Camera _camera;
        private RectTransform _rt;
        private RectTransform _canvasRt;
        private bool _isOverlay;
        private Camera _canvasCamera;
        private float _heightOffset;

        private Vector2 _lastLocalPos;
        private bool _positionValid;

        public void Initialize(Transform enemyTransform, Camera camera, Canvas parentCanvas)
        {
            if (_config == null)
                Debug.LogError("[EnemyWorldToScreenPositioner] SuspicionIndicatorConfig not assigned!", this);

            _enemyTransform = enemyTransform;
            _heightOffset = _config != null ? _config.worldHeightOffset : 2.4f;
            _camera = camera;
            _rt = GetComponent<RectTransform>();
            _canvasRt = parentCanvas.GetComponent<RectTransform>();
            _isOverlay = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
            _canvasCamera = _isOverlay ? null : parentCanvas.worldCamera;
            _positionValid = false;
        }

        private void LateUpdate()
        {
            if (_enemyTransform == null) return;

            Vector3 world = _enemyTransform.position + new Vector3(0f, _heightOffset, 0f);
            Vector3 screen = _camera.WorldToScreenPoint(world);

            if (screen.z < 0f)
            {
                if (_positionValid)
                {
                    _positionValid = false;
                    gameObject.SetActive(false);
                }
                return;
            }

            if (!_positionValid)
            {
                _positionValid = true;
                gameObject.SetActive(true);
            }

            Vector2 localPos;
            if (_isOverlay)
            {
                localPos = screen;
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRt, screen, _canvasCamera, out localPos);
            }

            if (localPos == _lastLocalPos) return;
            _lastLocalPos = localPos;

            if (_isOverlay)
                _rt.position = screen;
            else
                _rt.localPosition = localPos;
        }
    }
}