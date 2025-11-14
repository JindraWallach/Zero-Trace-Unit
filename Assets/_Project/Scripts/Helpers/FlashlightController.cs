using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

public class FlashlightController : MonoBehaviour, IInitializable
{
    [SerializeField] private Light _light;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material _offMat;
    [SerializeField] private Material _onMat;

    private bool _isOn;
    private InputReader _inputReader;

    public void Initialize(DependencyInjector di)
    {
        _inputReader = di.InputReader;
        _inputReader.onFlashlightToggled += ToggleFlashlight;
    }

    private void OnDestroy()
    {
        if (_inputReader != null)
            _inputReader.onFlashlightToggled -= ToggleFlashlight;
    }

    private void ToggleFlashlight()
    {
        _isOn = !_isOn;

        _light.enabled = _isOn;
        _renderer.material = _isOn ? _onMat : _offMat;
    }
}
