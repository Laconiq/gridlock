using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public static class NeonWireframe
    {
        public static void Pyramid(Vector3 center, float size, Color color, float rotY = 0f)
        {
            float r = size * 0.5f;
            float baseY = center.Y - size * 0.25f;
            float tipY = center.Y + size * 0.4f;

            var tip = new Vector3(center.X, tipY, center.Z);
            Span<Vector3> baseVerts = stackalloc Vector3[4];
            ComputeSquareBase(center.X, baseY, center.Z, r, rotY, baseVerts);

            for (int i = 0; i < 4; i++)
            {
                Rendering.LineBatch.ThickLine3D(baseVerts[i], baseVerts[(i + 1) % 4], color);
                Rendering.LineBatch.ThickLine3D(baseVerts[i], tip, color);
            }
        }

        public static void PyramidThick(Vector3 center, float size, Color color, float rotY = 0f)
        {
            Pyramid(center, size, color, rotY);
            Pyramid(center, size * 0.97f, color, rotY);
            Pyramid(center, size * 1.03f, color, rotY);
        }

        public static void PyramidGlow(Vector3 center, float size, Color baseColor, byte alpha, float rotY = 0f)
        {
            var glowColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Pyramid(center, size, glowColor, rotY);
            Raylib.EndBlendMode();
        }

        public static void Cube(Vector3 center, float sx, float sy, float sz, Color color)
        {
            float hx = sx * 0.5f, hy = sy * 0.5f, hz = sz * 0.5f;
            Span<Vector3> v = stackalloc Vector3[8];
            v[0] = center + new Vector3(-hx, -hy, -hz);
            v[1] = center + new Vector3( hx, -hy, -hz);
            v[2] = center + new Vector3( hx, -hy,  hz);
            v[3] = center + new Vector3(-hx, -hy,  hz);
            v[4] = center + new Vector3(-hx,  hy, -hz);
            v[5] = center + new Vector3( hx,  hy, -hz);
            v[6] = center + new Vector3( hx,  hy,  hz);
            v[7] = center + new Vector3(-hx,  hy,  hz);

            for (int i = 0; i < 4; i++)
            {
                Rendering.LineBatch.ThickLine3D(v[i], v[(i + 1) % 4], color);
                Rendering.LineBatch.ThickLine3D(v[i + 4], v[(i + 1) % 4 + 4], color);
                Rendering.LineBatch.ThickLine3D(v[i], v[i + 4], color);
            }
        }

        public static void Diamond(Vector3 center, float radiusH, float radiusV, Color color, float rotY = 0f)
        {
            var top = new Vector3(center.X, center.Y + radiusV, center.Z);
            var bot = new Vector3(center.X, center.Y - radiusV, center.Z);
            Span<Vector3> mid = stackalloc Vector3[4];
            ComputeSquareBase(center.X, center.Y, center.Z, radiusH, rotY, mid);

            for (int i = 0; i < 4; i++)
            {
                Rendering.LineBatch.ThickLine3D(mid[i], mid[(i + 1) % 4], color);
                Rendering.LineBatch.ThickLine3D(mid[i], top, color);
                Rendering.LineBatch.ThickLine3D(mid[i], bot, color);
            }
        }

        private static void ComputeSquareBase(float cx, float y, float cz, float radius, float rotY, Span<Vector3> verts)
        {
            float cos = MathF.Cos(rotY);
            float sin = MathF.Sin(rotY);

            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 0.5f + 0.25f) * MathF.PI;
                float lx = MathF.Cos(angle) * radius;
                float lz = MathF.Sin(angle) * radius;
                verts[i] = new Vector3(
                    cx + lx * cos - lz * sin,
                    y,
                    cz + lx * sin + lz * cos);
            }
        }
    }
}
