using UnityEngine;
using System;

[Serializable]
public class HackWaveSettings
{
    [Range(0.1f, 5f)] public float waveDuration = 1f;
    [Range(0f, 0.5f)] public float distortionStrength = 0.1f;
    [Range(0.01f, 0.5f)] public float waveWidth = 0.15f;
    [Range(0f, 0.02f)] public float blurAmount = 0.008f;
    public bool looping = false;
    [Range(0f, 5f)] public float loopDelay = 2f;
}

public class HackWaveController : MonoBehaviour
{
    [SerializeField] private Material hackWaveMaterial;
    [SerializeField] private HackWaveSettings settings = new HackWaveSettings();

    private float currentProgress;
    private bool isPlaying;
    private float loopTimer;

    // Cached property IDs
    private static class ShaderIDs
    {
        public static readonly int WaveProgress = Shader.PropertyToID("_WaveProgress");
        public static readonly int DistortionStrength = Shader.PropertyToID("_DistortionStrength");
        public static readonly int WaveWidth = Shader.PropertyToID("_WaveWidth");
        public static readonly int BlurAmount = Shader.PropertyToID("_BlurAmount");
    }

    private void Awake()
    {
        ValidateMaterial();
        UpdateMaterialProperties();
    }

    private void OnEnable()
    {
        ResetWave();
    }

    private void OnDisable()
    {
        StopWave();
    }

    private void Update()
    {
        if (isPlaying)
        {
            UpdateWaveProgress(Time.deltaTime);
        }
        else if (settings.looping && loopTimer > 0f)
        {
            UpdateLoopTimer(Time.deltaTime);
        }
    }

    private void UpdateWaveProgress(float deltaTime)
    {
        currentProgress += deltaTime / settings.waveDuration;

        if (currentProgress >= 1f)
        {
            currentProgress = 1f;
            isPlaying = false;

            if (settings.looping)
            {
                loopTimer = settings.loopDelay;
            }
        }

        hackWaveMaterial.SetFloat(ShaderIDs.WaveProgress, currentProgress);
    }

    private void UpdateLoopTimer(float deltaTime)
    {
        loopTimer -= deltaTime;
        if (loopTimer <= 0f)
        {
            TriggerWave();
        }
    }

    /// <summary>
    /// Spustí hack wave efekt
    /// </summary>
    public void TriggerWave()
    {
        if (!ValidateMaterial()) return;

        currentProgress = 0f;
        isPlaying = true;
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Zastaví probíhající vlnu
    /// </summary>
    public void StopWave()
    {
        isPlaying = false;
        ResetWave();
    }

    /// <summary>
    /// Resetuje vlnu do výchozího stavu
    /// </summary>
    private void ResetWave()
    {
        currentProgress = 0f;
        if (hackWaveMaterial != null)
        {
            hackWaveMaterial.SetFloat(ShaderIDs.WaveProgress, 0f);
        }
    }

    /// <summary>
    /// Aktualizuje parametry materiálu podle nastavení
    /// </summary>
    public void UpdateMaterialProperties()
    {
        if (!ValidateMaterial()) return;

        hackWaveMaterial.SetFloat(ShaderIDs.DistortionStrength, settings.distortionStrength);
        hackWaveMaterial.SetFloat(ShaderIDs.WaveWidth, settings.waveWidth);
        hackWaveMaterial.SetFloat(ShaderIDs.BlurAmount, settings.blurAmount);
    }

    /// <summary>
    /// Změní nastavení efektu za běhu
    /// </summary>
    public void SetSettings(HackWaveSettings newSettings)
    {
        settings = newSettings;
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Validuje, zda je material přiřazen
    /// </summary>
    private bool ValidateMaterial()
    {
        if (hackWaveMaterial == null)
        {
            Debug.LogError($"HackWave Material není přiřazen na {gameObject.name}!", this);
            return false;
        }
        return true;
    }

    private void OnValidate()
    {
        if (Application.isPlaying && hackWaveMaterial != null)
        {
            UpdateMaterialProperties();
        }
    }

    // Public gettery pro debugging/UI
    public bool IsPlaying => isPlaying;
    public float Progress => currentProgress;
    public HackWaveSettings CurrentSettings => settings;
}