using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Optimized URP Render Feature for screen glitch effect.
/// Uses ScriptableObject for settings and frame skipping for performance.
/// </summary>
public class ScreenGlitchFeature : ScriptableRendererFeature
{
    [Header("Settings")]
    [SerializeField] private GlitchEffectSettings settingsAsset;

    [Header("Fallback (if no SO assigned)")]
    [SerializeField] private bool useFallbackSettings = true;
    [SerializeField] private float fallbackIntensity = 0.02f;

    [Header("Shader")]
    [SerializeField] private Shader glitchShader;

    private Material glitchMaterial;
    private GlitchRenderPass glitchPass;

    // Cache for performance
    private int frameCounter = 0;

    public GlitchEffectSettings SettingsAsset => settingsAsset;

    public override void Create()
    {
        if (glitchShader == null)
        {
            Debug.LogError("[ScreenGlitchFeature] Shader not assigned!");
            return;
        }

        glitchMaterial = CoreUtils.CreateEngineMaterial(glitchShader);

        if (glitchMaterial == null)
        {
            Debug.LogError("[ScreenGlitchFeature] Failed to create material!");
            return;
        }

        glitchPass = new GlitchRenderPass(glitchMaterial);
        glitchPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

      //  Debug.Log("[ScreenGlitchFeature] Created successfully");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (glitchMaterial == null)
            return;

        // Skip for scene view
        if (renderingData.cameraData.isSceneViewCamera)
            return;

        // Get settings (SO or fallback)
        GlitchEffectSettings settings = settingsAsset;

        if (settings == null)
        {
            if (!useFallbackSettings)
                return;

            // Use fallback values
            settings = ScriptableObject.CreateInstance<GlitchEffectSettings>();
            settings.intensity = fallbackIntensity;
        }

        // Skip if disabled
        if (!settings.enabled || settings.intensity < 0.001f)
            return;

        // Frame skipping for performance
        frameCounter++;
        if (frameCounter % settings.updateFrequency != 0)
            return;

        glitchPass.Setup(settings);
        glitchPass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(glitchPass);
    }

    protected override void Dispose(bool disposing)
    {
        glitchPass?.Dispose();
        CoreUtils.Destroy(glitchMaterial);
    }

    // === RENDER PASS (OPTIMIZED) ===
    private class GlitchRenderPass : ScriptableRenderPass
    {
        private Material material;
        private GlitchEffectSettings currentSettings;

        // Cached property IDs (faster than string lookups)
        private static readonly int intensityID = Shader.PropertyToID("_Intensity");
        private static readonly int timeScaleID = Shader.PropertyToID("_TimeScale");
        private static readonly int colorShiftID = Shader.PropertyToID("_ColorShift");
        private static readonly int blockSizeID = Shader.PropertyToID("_BlockSize");
        private static readonly int scanlineIntensityID = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int inversionIntensityID = Shader.PropertyToID("_InversionIntensity");
        private static readonly int verticalShiftID = Shader.PropertyToID("_VerticalShift");
        private static readonly int noiseFrequencyID = Shader.PropertyToID("_NoiseFrequency");
        private static readonly int blitTextureID = Shader.PropertyToID("_BlitTexture");

        private class PassData
        {
            internal Material material;
            internal TextureHandle source;
            internal GlitchEffectSettings settings;
        }

        public GlitchRenderPass(Material mat)
        {
            this.material = mat;
            profilingSampler = new ProfilingSampler("ScreenGlitch");
        }

        public void Setup(GlitchEffectSettings settings)
        {
            currentSettings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (material == null || currentSettings == null)
                return;

            if (!resourceData.activeColorTexture.IsValid())
                return;

            // Early exit if intensity is zero (skip entire pass)
            if (currentSettings.intensity < 0.001f)
                return;

            // Get source
            TextureHandle source = resourceData.activeColorTexture;

            // Create temp texture (reuse descriptor for efficiency)
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                descriptor,
                "_GlitchTemp",
                false
            );

            // === MAIN GLITCH PASS ===
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("ScreenGlitchPass", out var passData, profilingSampler))
            {
                passData.material = material;
                passData.source = source;
                passData.settings = currentSettings;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (data.material == null || data.settings == null)
                        return;

                    // Batch property updates (fewer state changes)
                    data.material.SetFloat(intensityID, data.settings.intensity);
                    data.material.SetFloat(timeScaleID, data.settings.timeScale);
                    data.material.SetFloat(colorShiftID, data.settings.colorShift);
                    data.material.SetFloat(blockSizeID, data.settings.blockSize);
                    data.material.SetFloat(scanlineIntensityID, data.settings.scanlineIntensity);
                    data.material.SetFloat(inversionIntensityID, data.settings.inversionIntensity);
                    data.material.SetFloat(verticalShiftID, data.settings.verticalShift);
                    data.material.SetFloat(noiseFrequencyID, data.settings.noiseFrequency);
                    data.material.SetTexture(blitTextureID, data.source);

                    // Single blit call
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // === COPY BACK (optimized) ===
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("ScreenGlitchCopy", out var passData, profilingSampler))
            {
                passData.source = destination;

                builder.UseTexture(destination, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Fast copy without material
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        public void Dispose()
        {
            // Cleanup handled by RenderGraph
        }
    }
}