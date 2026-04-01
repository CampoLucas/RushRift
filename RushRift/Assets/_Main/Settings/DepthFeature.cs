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
            public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public Settings settings = new Settings();

        private PrecipiceDepthPass _pass;
        

        public override void Create()
        {
            _pass = new PrecipiceDepthPass(settings.material)
            {
                renderPassEvent = settings.passEvent
            };
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (settings.material == null) return;

            _pass.SetMaterial(settings.material);
            _pass.SetTarget(renderer.cameraColorTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.material == null) return;

            var cameraData = renderingData.cameraData;

            var cannotRenderPass = false;
#if DEPTH_FEATURE_IN_SCENE_VIEW
            cannotRenderPass = cameraData.isPreviewCamera;
#else
            cannotRenderPass = cameraData.isSceneViewCamera || cameraData.isPreviewCamera;
#endif
            if (cannotRenderPass) return;
            
            _pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _pass?.Dispose();
        }

        private class PrecipiceDepthPass : ScriptableRenderPass
        {
            private Material _material;
            private RTHandle _source;
            private RTHandle _tempRT;

            public PrecipiceDepthPass(Material material)
            {
                _material = material;
                //renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
                //ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);
            }

            public void SetMaterial(Material material)
            {
                _material = material;
            }

            public void SetTarget(RTHandle source)
            {
                _source = source;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                
                //RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, name: "_PrecipiceTempRT");
                RenderingUtils.ReAllocateIfNeeded(ref _tempRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_PrecipiceTempRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_material == null) return;
                if (_source == null) return;
                
                
                var cmd = CommandBufferPool.Get("DepthFeature");

                using (new ProfilingScope(cmd, new ProfilingSampler("DepthFeature")))
                {
                    Blitter.BlitCameraTexture(cmd, _source, _tempRT, _material, 0);
                    Blitter.BlitCameraTexture(cmd, _tempRT, _source);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            // public override void OnCameraCleanup(CommandBuffer cmd)
            // {
            //     
            // }

            public void Dispose()
            {
                _tempRT?.Release();
                _tempRT = null;
                _source = null;
            }
        }
    }
}