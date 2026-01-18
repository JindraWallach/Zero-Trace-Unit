using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

public class FlashlightController : MonoBehaviour, IInitializable
{
    [SerializeField] private Light _light;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material _offMat;
    [SerializeField] private Material _onMat;

    [Header("Noise")]
    [SerializeField] private NoiseConfig noiseConfig;
    [SerializeField] private NoiseEmitter noiseEmitter;
    [SerializeField] private Transform noiseOrigin;

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

    private void Start()
    {
        // Initialize state from the Light component so we don't start out-of-sync
        _isOn = _light != null && _light.enabled;

        if (_renderer != null)
            _renderer.material = _isOn ? _onMat : _offMat;
    }

    private void ToggleFlashlight()
    {
        _isOn = !_isOn;

        _light.enabled = _isOn;
        _renderer.material = _isOn ? _onMat : _offMat;

        noiseEmitter.EmitFlashlightSound(noiseOrigin.position);
        //Debug.Log($"[FlashlightController] Flashlight toggled {_isOn}. Emitted noise: {noiseType}");
    }
}