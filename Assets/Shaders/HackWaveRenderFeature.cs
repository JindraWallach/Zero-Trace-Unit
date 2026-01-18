using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class HackWaveRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material hackWaveMaterial;
    }

    public Settings settings = new Settings();
    private HackWaveRenderPass hackWavePass;

    public override void Create()
    {
        if (settings.hackWaveMaterial == null)
        {
            Debug.LogWarning("HackWave Material není přiřazen!");
            return;
        }

        hackWavePass = new HackWaveRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (hackWavePass == null || settings.hackWaveMaterial == null) return;

        renderer.EnqueuePass(hackWavePass);
    }

    protected override void Dispose(bool disposing)
    {
        hackWavePass?.Dispose();
    }

    class HackWaveRenderPass : ScriptableRenderPass
    {
        private Settings settings;
        private static readonly int TempTextureID = Shader.PropertyToID("_HackWaveTempRT");

        // Render Graph data
        private class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle destination;
        }

        public HackWaveRenderPass(Settings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
            requiresIntermediateTexture = true;
        }

        // Render Graph API
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (settings.hackWaveMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (!resourceData.isActiveTargetBackBuffer)
            {
                TextureHandle source = resourceData.activeColorTexture;

                if (!source.IsValid()) return;

                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;

                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, descriptor, "_HackWaveDestination", false);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("HackWave Effect", out var passData))
                {
                    passData.material = settings.hackWaveMaterial;
                    passData.source = source;
                    passData.destination = destination;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0),
                            data.material, 0);
                    });
                }

                // Copy back to source
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("HackWave Copy Back", out var passData))
                {
                    passData.source = destination;
                    passData.destination = source;

                    builder.UseTexture(destination, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0),
                            null, 0);
                    });
                }
            }
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}