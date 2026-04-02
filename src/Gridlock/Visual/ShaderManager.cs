using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class ShaderManager
    {
        public static ShaderManager? Instance { get; private set; }

        private const string BasePath = "resources/shaders/glsl330/";

        public Shader GridShader { get; private set; }
        public Shader GlowShader { get; private set; }
        public Shader OutlineShader { get; private set; }
        public Shader ProjectileShader { get; private set; }
        public Shader TrailShader { get; private set; }

        private bool _initialized;

        public void Init()
        {
            Instance = this;

            GridShader = TryLoadShader("cybergrid.vs", "cybergrid.fs");
            GlowShader = TryLoadShader("fullscreen.vs", "glow.fs");
            OutlineShader = TryLoadShader("fullscreen.vs", "outline.fs");
            ProjectileShader = TryLoadShader("fullscreen.vs", "projectile.fs");
            TrailShader = TryLoadShader("fullscreen.vs", "trail.fs");

            _initialized = true;
        }

        public void Shutdown()
        {
            if (_initialized)
            {
                UnloadIfValid(GridShader);
                UnloadIfValid(GlowShader);
                UnloadIfValid(OutlineShader);
                UnloadIfValid(ProjectileShader);
                UnloadIfValid(TrailShader);
                _initialized = false;
            }

            if (Instance == this) Instance = null;
        }

        public static void SetFloat(Shader shader, string name, float value)
        {
            int loc = Raylib.GetShaderLocation(shader, name);
            if (loc >= 0)
                Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Float);
        }

        public static void SetVec2(Shader shader, string name, float[] value)
        {
            int loc = Raylib.GetShaderLocation(shader, name);
            if (loc >= 0)
                Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec2);
        }

        public static void SetVec4(Shader shader, string name, float[] value)
        {
            int loc = Raylib.GetShaderLocation(shader, name);
            if (loc >= 0)
                Raylib.SetShaderValue(shader, loc, value, ShaderUniformDataType.Vec4);
        }

        public static void SetMatrix(Shader shader, string name, Matrix4x4 value)
        {
            int loc = Raylib.GetShaderLocation(shader, name);
            if (loc >= 0)
                Raylib.SetShaderValueMatrix(shader, loc, value);
        }

        private static Shader TryLoadShader(string vsFile, string fsFile)
        {
            string vsPath = BasePath + vsFile;
            string fsPath = BasePath + fsFile;

            if (System.IO.File.Exists(vsPath) && System.IO.File.Exists(fsPath))
                return Raylib.LoadShader(vsPath, fsPath);

            return Raylib.LoadShaderFromMemory((string)null!, (string)null!);
        }

        private static void UnloadIfValid(Shader shader)
        {
            if (shader.Id > 0)
                Raylib.UnloadShader(shader);
        }
    }
}
