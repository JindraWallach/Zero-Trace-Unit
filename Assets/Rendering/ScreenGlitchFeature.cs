using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Modern URP Render Feature for screen glitch effect.
/// Compatible with Unity 6 / URP 17+ using RenderGraph API.
/// </summary>
public class ScreenGlitchFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(0f, 0.1f)] public float intensity = 0.02f;
        [Range(0f, 10f)] public float timeScale = 1f;
        [Range(0f, 0.05f)] public float colorShift = 0.01f;
        [Range(1f, 100f)] public float blockSize = 20f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    [SerializeField] private Settings settings = new Settings();
    [SerializeField] private Shader glitchShader;

    private Material glitchMaterial;
    private GlitchRenderPass glitchPass;

    public Settings GetSettings() => settings;

    public override void Create()
    {
        if (glitchShader == null)
        {
            Debug.LogError("[ScreenGlitchFeature] Shader not assigned!");
            return;
        }

        glitchMaterial = CoreUtils.CreateEngineMaterial(glitchShader);
        glitchPass = new GlitchRenderPass(glitchMaterial, settings);
        glitchPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (glitchMaterial == null)
        {
            Debug.LogWarning("[ScreenGlitchFeature] Material not created!");
            return;
        }

        // Skip for scene view if desired
        if (renderingData.cameraData.isSceneViewCamera)
            return;

        glitchPass.ConfigureInput(ScriptableRenderPassInput.Color);
        renderer.EnqueuePass(glitchPass);
    }

    protected override void Dispose(bool disposing)
    {
        glitchPass?.Dispose();
        CoreUtils.Destroy(glitchMaterial);
    }

    // === RENDER PASS ===
    private class GlitchRenderPass : ScriptableRenderPass
    {
        private Material material;
        private Settings settings;

        private static readonly int intensityID = Shader.PropertyToID("_Intensity");
        private static readonly int timeScaleID = Shader.PropertyToID("_TimeScale");
        private static readonly int colorShiftID = Shader.PropertyToID("_ColorShift");
        private static readonly int blockSizeID = Shader.PropertyToID("_BlockSize");

        public GlitchRenderPass(Material mat, Settings settings)
        {
            this.material = mat;
            this.settings = settings;
            profilingSampler = new ProfilingSampler("ScreenGlitch");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Check if source texture is valid (FIXED: removed .rt check)
            if (material == null || !resourceData.activeColorTexture.IsValid())
                return;

            // Setup material properties
            material.SetFloat(intensityID, settings.intensity);
            material.SetFloat(timeScaleID, settings.timeScale);
            material.SetFloat(colorShiftID, settings.colorShift);
            material.SetFloat(blockSizeID, settings.blockSize);

            // Create temporary texture descriptor
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph,
                descriptor,
                "_GlitchTemp",
                false
            );

            // Add blit pass with material
            RenderGraphUtils.BlitMaterialParameters blitParams = new RenderGraphUtils.BlitMaterialParameters(
                source,
                destination,
                material,
                0
            );

            renderGraph.AddBlitPass(blitParams, passName: "ScreenGlitchBlit");

            // Copy result back to camera target
            RenderGraphUtils.BlitMaterialParameters copyParams = new RenderGraphUtils.BlitMaterialParameters(
                destination,
                source,
                null,
                0
            );

            renderGraph.AddBlitPass(copyParams, passName: "ScreenGlitchCopy");
        }

        public void Dispose()
        {
            // RTHandle cleanup no longer needed with RenderGraph
        }
    }
}