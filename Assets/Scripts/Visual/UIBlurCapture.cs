using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace Gridlock.Visual
{
    public class UIBlurCapture : ScriptableRendererFeature
    {
        [SerializeField, Range(1, 6)] private int iterations = 4;
        [SerializeField, Range(0f, 1f)] private float darken = 0.8f;

        private Material _material;
        private BlurPass _pass;

        public static RenderTexture BlurTexture { get; private set; }

        public override void Create()
        {
            var shader = Shader.Find("Hidden/Gridlock/DualKawaseBlur");
            if (shader != null)
                _material = CoreUtils.CreateEngineMaterial(shader);

            _pass = new BlurPass { renderPassEvent = RenderPassEvent.AfterRenderingOpaques };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;

            _pass.Setup(_material, iterations, darken);
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Cleanup();
            if (_material != null)
                CoreUtils.Destroy(_material);
        }

        private class BlurPass : ScriptableRenderPass
        {
            private Material _material;
            private int _iterations;
            private float _darken;
            private RTHandle _persistentRT;

            private static readonly int DarkenId = Shader.PropertyToID("_Darken");

            public void Setup(Material material, int iterations, float darken)
            {
                _material = material;
                _iterations = iterations;
                _darken = darken;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) return;

                var src = resourceData.activeColorTexture;
                if (!src.IsValid()) return;

                var srcDesc = src.GetDescriptor(renderGraph);
                int w = srcDesc.width;
                int h = srcDesc.height;

                var downChain = new TextureHandle[_iterations + 1];
                downChain[0] = src;

                for (int i = 0; i < _iterations; i++)
                {
                    w = Mathf.Max(1, w / 2);
                    h = Mathf.Max(1, h / 2);
                    var desc = new TextureDesc(w, h)
                    {
                        colorFormat = srcDesc.colorFormat,
                        depthBufferBits = DepthBits.None,
                        filterMode = FilterMode.Bilinear,
                        name = $"BlurDown{i}"
                    };
                    downChain[i + 1] = renderGraph.CreateTexture(desc);

                    var blitParams = new RenderGraphUtils.BlitMaterialParameters(
                        downChain[i], downChain[i + 1], _material, 0);
                    RenderGraphUtils.AddBlitPass(renderGraph, blitParams, $"DualKawaseDown{i}");
                }

                var current = downChain[_iterations];
                for (int i = _iterations - 1; i >= 0; i--)
                {
                    w = Mathf.Max(1, w * 2);
                    h = Mathf.Max(1, h * 2);
                    var desc = new TextureDesc(w, h)
                    {
                        colorFormat = srcDesc.colorFormat,
                        depthBufferBits = DepthBits.None,
                        filterMode = FilterMode.Bilinear,
                        name = $"BlurUp{i}"
                    };
                    var upTarget = renderGraph.CreateTexture(desc);

                    var blitParams = new RenderGraphUtils.BlitMaterialParameters(
                        current, upTarget, _material, 1);
                    RenderGraphUtils.AddBlitPass(renderGraph, blitParams, $"DualKawaseUp{i}");
                    current = upTarget;
                }

                EnsurePersistentRT(srcDesc.width, srcDesc.height);
                var imported = renderGraph.ImportTexture(_persistentRT);

                _material.SetFloat(DarkenId, _darken);
                var darkenParams = new RenderGraphUtils.BlitMaterialParameters(
                    current, imported, _material, 2);
                RenderGraphUtils.AddBlitPass(renderGraph, darkenParams, "BlurDarken");

                UIBlurCapture.BlurTexture = _persistentRT.rt;
            }

            private void EnsurePersistentRT(int w, int h)
            {
                if (_persistentRT != null && _persistentRT.rt != null
                    && _persistentRT.rt.width == w && _persistentRT.rt.height == h)
                    return;

                _persistentRT?.Release();
                var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.Default, 0);
                _persistentRT = RTHandles.Alloc(desc, FilterMode.Bilinear, name: "UIBlurResult");
            }

            public void Cleanup()
            {
                _persistentRT?.Release();
                _persistentRT = null;
                UIBlurCapture.BlurTexture = null;
            }
        }
    }
}
