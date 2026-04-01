using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.URP
{
    public class DepthFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material material;
        }

        public Settings settings = new Settings();

        private PrecipiceDepthPass _pass;
        

        public override void Create()
        {
            _pass = new PrecipiceDepthPass(settings.material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null) return;
            renderer.EnqueuePass(_pass);
        }

        private class PrecipiceDepthPass : ScriptableRenderPass
        {
            private readonly Material _material;
            private RTHandle _tempRT;

            public PrecipiceDepthPass(Material material)
            {
                _material = material;
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                // This is what was missing — tells URP to generate the depth texture
                ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_PrecipiceTempRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null) return;

                var cmd = CommandBufferPool.Get("PrecipiceDepth");
                var renderer = renderingData.cameraData.renderer;

                // Blit from camera color into temp, applying our material
                Blitter.BlitCameraTexture(cmd, renderer.cameraColorTargetHandle, _tempRT, _material, 0);
                // Blit result back to camera color — no material, straight copy
                Blitter.BlitCameraTexture(cmd, _tempRT, renderer.cameraColorTargetHandle);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // nothing needed
            }

            public void Dispose()
            {
                _tempRT?.Release();
            }
        }
    }
}