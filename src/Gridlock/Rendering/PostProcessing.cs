using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Rendering
{
    public sealed class PostProcessing
    {
        private RenderTexture2D _sceneRT;
        private RenderTexture2D _compositeRT;
        private RenderTexture2D[] _bloomDown = Array.Empty<RenderTexture2D>();
        private RenderTexture2D[] _bloomUp = Array.Empty<RenderTexture2D>();

        private Shader _thresholdShader;
        private Shader _kawaseDownShader;
        private Shader _kawaseUpShader;
        private Shader _chromaticShader;
        private Shader _vignetteShader;
        private Shader _pixelGridShader;

        private int _thresholdLoc;
        private int _texelSizeDownLoc;
        private int _texelSizeUpLoc;
        private int _chromaticIntensityLoc;
        private int _vignetteIntensityLoc;
        private int _vignetteColorLoc;
        private int _pixelGridResolutionLoc;

        private int _screenW;
        private int _screenH;
        private int _internalW;
        private int _internalH;

        private bool _pixelGridShaderLoaded;

        public int PixelScale { get; set; } = 1;
        public float BloomThreshold { get; set; } = 0.6f;
        public float BloomIntensity { get; set; } = 1.5f;
        public float ChromaticIntensity { get; set; }
        public float VignetteIntensity { get; set; } = 0.3f;

        private RenderTexture2D _blurA, _blurB;
        private bool _hasBackdropBlur;
        public int ScreenWidth => _screenW;
        public int ScreenHeight => _screenH;

        public void DrawBackdropBlur(Rectangle panelRect, float darken = 0.5f)
        {
            if (!_hasBackdropBlur) return;

            var blur = _blurA.Texture;

            // Source rect from blur texture — Y flipped for OpenGL render texture
            var src = new Rectangle(0, 0, blur.Width, -blur.Height);
            // Draw blurred scene stretched to panel rect area
            byte tint = (byte)Math.Clamp((int)(255 * darken), 0, 255);
            Raylib.DrawTexturePro(blur, src, panelRect, Vector2.Zero, 0f,
                new Color(tint, tint, tint, (byte)255));
        }

        public void BuildBackdropBlur()
        {
            if (_kawaseDownShader.Id == 0) return;

            int bw = _internalW / 2;
            int bh = _internalH / 2;
            if (!_hasBackdropBlur)
            {
                _blurA = Raylib.LoadRenderTexture(bw, bh);
                _blurB = Raylib.LoadRenderTexture(bw / 2, bh / 2);
                _hasBackdropBlur = true;
            }

            // Downsample the composited scene 3 times for strong blur
            float[] t1 = { 1f / _compositeRT.Texture.Width, 1f / _compositeRT.Texture.Height };
            Raylib.SetShaderValue(_kawaseDownShader, _texelSizeDownLoc, t1, ShaderUniformDataType.Vec2);
            BlitPass(_blurA, _compositeRT.Texture, _kawaseDownShader);

            float[] t2 = { 1f / _blurA.Texture.Width, 1f / _blurA.Texture.Height };
            Raylib.SetShaderValue(_kawaseDownShader, _texelSizeDownLoc, t2, ShaderUniformDataType.Vec2);
            BlitPass(_blurB, _blurA.Texture, _kawaseDownShader);

            float[] t3 = { 1f / _blurB.Texture.Width, 1f / _blurB.Texture.Height };
            Raylib.SetShaderValue(_kawaseDownShader, _texelSizeDownLoc, t3, ShaderUniformDataType.Vec2);
            BlitPass(_blurA, _blurB.Texture, _kawaseDownShader);
        }

        const int BloomIterations = 4;
        const string ShaderPath = "resources/shaders/glsl330/";

        public int InternalWidth => _internalW;
        public int InternalHeight => _internalH;

        public void Init(int screenW, int screenH)
        {
            _screenW = screenW;
            _screenH = screenH;
            _internalW = screenW / PixelScale;
            _internalH = screenH / PixelScale;

            _sceneRT = Raylib.LoadRenderTexture(_internalW, _internalH);
            Raylib.SetTextureFilter(_sceneRT.Texture, TextureFilter.Point);

            _compositeRT = Raylib.LoadRenderTexture(_internalW, _internalH);
            Raylib.SetTextureFilter(_compositeRT.Texture, TextureFilter.Point);

            _bloomDown = new RenderTexture2D[BloomIterations];
            _bloomUp = new RenderTexture2D[BloomIterations];

            int w = _internalW / 2;
            int h = _internalH / 2;
            for (int i = 0; i < BloomIterations; i++)
            {
                w = Math.Max(1, w);
                h = Math.Max(1, h);
                _bloomDown[i] = Raylib.LoadRenderTexture(w, h);
                _bloomUp[i] = Raylib.LoadRenderTexture(w, h);
                w /= 2;
                h /= 2;
            }

            _thresholdShader = Raylib.LoadShader(ShaderPath + "fullscreen.vs", ShaderPath + "threshold.fs");
            _kawaseDownShader = Raylib.LoadShader(ShaderPath + "fullscreen.vs", ShaderPath + "kawase_down.fs");
            _kawaseUpShader = Raylib.LoadShader(ShaderPath + "fullscreen.vs", ShaderPath + "kawase_up.fs");
            _chromaticShader = Raylib.LoadShader(ShaderPath + "fullscreen.vs", ShaderPath + "chromatic.fs");
            _vignetteShader = Raylib.LoadShader(ShaderPath + "fullscreen.vs", ShaderPath + "vignette.fs");
            _pixelGridShader = Raylib.LoadShader(ShaderPath + "fullscreen.vs", ShaderPath + "pixelgrid.fs");

            _thresholdLoc = Raylib.GetShaderLocation(_thresholdShader, "threshold");
            _texelSizeDownLoc = Raylib.GetShaderLocation(_kawaseDownShader, "texelSize");
            _texelSizeUpLoc = Raylib.GetShaderLocation(_kawaseUpShader, "texelSize");
            _chromaticIntensityLoc = Raylib.GetShaderLocation(_chromaticShader, "intensity");
            _vignetteIntensityLoc = Raylib.GetShaderLocation(_vignetteShader, "intensity");
            _vignetteColorLoc = Raylib.GetShaderLocation(_vignetteShader, "vignetteColor");
            _pixelGridResolutionLoc = Raylib.GetShaderLocation(_pixelGridShader, "resolution");

            _pixelGridShaderLoaded = _pixelGridShader.Id > 0;
        }

        public void BeginScene()
        {
            Raylib.BeginTextureMode(_sceneRT);
        }

        public void EndSceneAndComposite()
        {
            Raylib.SetShaderValue(_thresholdShader, _thresholdLoc, BloomThreshold, ShaderUniformDataType.Float);
            BlitPass(_bloomDown[0], _sceneRT.Texture, _thresholdShader);

            for (int i = 1; i < BloomIterations; i++)
            {
                float[] texel = { 1f / _bloomDown[i - 1].Texture.Width, 1f / _bloomDown[i - 1].Texture.Height };
                Raylib.SetShaderValue(_kawaseDownShader, _texelSizeDownLoc, texel, ShaderUniformDataType.Vec2);
                BlitPass(_bloomDown[i], _bloomDown[i - 1].Texture, _kawaseDownShader);
            }

            _bloomUp[BloomIterations - 1] = _bloomDown[BloomIterations - 1];
            for (int i = BloomIterations - 2; i >= 0; i--)
            {
                float[] texel = { 1f / _bloomUp[i + 1].Texture.Width, 1f / _bloomUp[i + 1].Texture.Height };
                Raylib.SetShaderValue(_kawaseUpShader, _texelSizeUpLoc, texel, ShaderUniformDataType.Vec2);
                BlitPass(_bloomUp[i], _bloomUp[i + 1].Texture, _kawaseUpShader);
            }

            Raylib.BeginTextureMode(_compositeRT);
            Raylib.ClearBackground(Color.Black);
            DrawFlipped(_sceneRT.Texture, _internalW, _internalH, Color.White);
            if (BloomIntensity > 0f)
            {
                Raylib.BeginBlendMode(BlendMode.Additive);
                byte a = (byte)Math.Clamp((int)(BloomIntensity / 2.5f * 255f), 0, 255);
                DrawFlipped(_bloomUp[0].Texture, _internalW, _internalH, new Color((byte)255, (byte)255, (byte)255, a));
                Raylib.EndBlendMode();
            }
            Raylib.EndTextureMode();

            if (ChromaticIntensity > 0.001f)
            {
                Raylib.SetShaderValue(_chromaticShader, _chromaticIntensityLoc, ChromaticIntensity, ShaderUniformDataType.Float);
                BlitPass(_sceneRT, _compositeRT.Texture, _chromaticShader);
                (_compositeRT, _sceneRT) = (_sceneRT, _compositeRT);
            }

            if (VignetteIntensity > 0.001f)
            {
                Raylib.SetShaderValue(_vignetteShader, _vignetteIntensityLoc, VignetteIntensity, ShaderUniformDataType.Float);
                float[] vigColor = { 0f, 0.05f, 0.1f };
                Raylib.SetShaderValue(_vignetteShader, _vignetteColorLoc, vigColor, ShaderUniformDataType.Vec3);
                BlitPass(_sceneRT, _compositeRT.Texture, _vignetteShader);
                (_compositeRT, _sceneRT) = (_sceneRT, _compositeRT);
            }

        }

        public void DrawFinalToScreen()
        {
            Raylib.SetTextureFilter(_compositeRT.Texture, TextureFilter.Point);
            if (_pixelGridShaderLoaded)
            {
                float[] res = { _internalW, _internalH };
                Raylib.SetShaderValue(_pixelGridShader, _pixelGridResolutionLoc, res, ShaderUniformDataType.Vec2);
                Raylib.BeginShaderMode(_pixelGridShader);
            }
            DrawFlipped(_compositeRT.Texture, _screenW, _screenH, Color.White);
            if (_pixelGridShaderLoaded)
                Raylib.EndShaderMode();
        }

        private static void BlitPass(RenderTexture2D target, Texture2D source, Shader shader)
        {
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(Color.Black);
            Raylib.BeginShaderMode(shader);
            var src = new Rectangle(0, 0, source.Width, -source.Height);
            var dst = new Rectangle(0, 0, target.Texture.Width, target.Texture.Height);
            Raylib.DrawTexturePro(source, src, dst, System.Numerics.Vector2.Zero, 0f, Color.White);
            Raylib.EndShaderMode();
            Raylib.EndTextureMode();
        }

        private static void DrawFlipped(Texture2D texture, int destW, int destH, Color tint)
        {
            var src = new Rectangle(0, 0, texture.Width, -texture.Height);
            var dst = new Rectangle(0, 0, destW, destH);
            Raylib.DrawTexturePro(texture, src, dst, System.Numerics.Vector2.Zero, 0f, tint);
        }

        public void OnResize(int w, int h)
        {
            if (w == _screenW && h == _screenH) return;
            UnloadRTs();
            Init(w, h);
        }

        public void Shutdown()
        {
            UnloadRTs();
            Raylib.UnloadShader(_thresholdShader);
            Raylib.UnloadShader(_kawaseDownShader);
            Raylib.UnloadShader(_kawaseUpShader);
            Raylib.UnloadShader(_chromaticShader);
            Raylib.UnloadShader(_vignetteShader);
            if (_pixelGridShaderLoaded)
                Raylib.UnloadShader(_pixelGridShader);
        }

        private void UnloadRTs()
        {
            Raylib.UnloadRenderTexture(_sceneRT);
            Raylib.UnloadRenderTexture(_compositeRT);
            for (int i = 0; i < _bloomDown.Length; i++)
                Raylib.UnloadRenderTexture(_bloomDown[i]);
            for (int i = 0; i < _bloomUp.Length; i++)
                Raylib.UnloadRenderTexture(_bloomUp[i]);
            if (_hasBackdropBlur)
            {
                Raylib.UnloadRenderTexture(_blurA);
                Raylib.UnloadRenderTexture(_blurB);
                _hasBackdropBlur = false;
            }
        }
    }
}
