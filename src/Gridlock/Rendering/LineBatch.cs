using System;
using System.Numerics;
using Raylib_cs;
using Color = Raylib_cs.Color;

namespace Gridlock.Rendering
{
    public sealed class LineBatch
    {
        private const int MaxLines = 8192;
        private const int GlLines = 0x0001;

        private const float Sqrt2Over2 = 0.70710678f;

        private readonly Vector3[] _positions;
        private readonly Color[] _colors;
        private int _count;

        public LineBatch()
        {
            _positions = new Vector3[MaxLines * 2];
            _colors = new Color[MaxLines * 2];
        }

        public void Begin()
        {
            _count = 0;
        }

        public void Line(Vector3 a, Vector3 b, Color color)
        {
            if (_count >= MaxLines) Flush();
            int i = _count * 2;
            _positions[i] = a;
            _positions[i + 1] = b;
            _colors[i] = color;
            _colors[i + 1] = color;
            _count++;
        }

        public void CubeWires(Vector3 center, float sx, float sy, float sz, Color color)
        {
            float hx = sx * 0.5f, hy = sy * 0.5f, hz = sz * 0.5f;

            var v0 = new Vector3(center.X - hx, center.Y - hy, center.Z - hz);
            var v1 = new Vector3(center.X + hx, center.Y - hy, center.Z - hz);
            var v2 = new Vector3(center.X + hx, center.Y - hy, center.Z + hz);
            var v3 = new Vector3(center.X - hx, center.Y - hy, center.Z + hz);
            var v4 = new Vector3(center.X - hx, center.Y + hy, center.Z - hz);
            var v5 = new Vector3(center.X + hx, center.Y + hy, center.Z - hz);
            var v6 = new Vector3(center.X + hx, center.Y + hy, center.Z + hz);
            var v7 = new Vector3(center.X - hx, center.Y + hy, center.Z + hz);

            Line(v0, v1, color); Line(v1, v2, color); Line(v2, v3, color); Line(v3, v0, color);
            Line(v4, v5, color); Line(v5, v6, color); Line(v6, v7, color); Line(v7, v4, color);
            Line(v0, v4, color); Line(v1, v5, color); Line(v2, v6, color); Line(v3, v7, color);
        }

        public void OctahedronWires(Vector3 center, float radiusH, float radiusV, float angleY, Color color)
        {
            float cos = MathF.Cos(angleY);
            float sin = MathF.Sin(angleY);

            var top = new Vector3(center.X, center.Y + radiusV, center.Z);
            var bot = new Vector3(center.X, center.Y - radiusV, center.Z);
            var right = new Vector3(center.X + radiusH * cos, center.Y, center.Z + radiusH * sin);
            var left  = new Vector3(center.X - radiusH * cos, center.Y, center.Z - radiusH * sin);
            var front = new Vector3(center.X - radiusH * sin, center.Y, center.Z + radiusH * cos);
            var back  = new Vector3(center.X + radiusH * sin, center.Y, center.Z - radiusH * cos);

            Line(top, right, color); Line(top, left, color); Line(top, front, color); Line(top, back, color);
            Line(bot, right, color); Line(bot, left, color); Line(bot, front, color); Line(bot, back, color);
            Line(right, front, color); Line(front, left, color); Line(left, back, color); Line(back, right, color);
        }

        public void PyramidWires(Vector3 center, float size, Color color)
        {
            float r = size * 0.5f;
            float baseY = center.Y - size * 0.25f;
            float tipY = center.Y + size * 0.4f;
            var tip = new Vector3(center.X, tipY, center.Z);

            float d = Sqrt2Over2 * r;
            var b0 = new Vector3(center.X + d, baseY, center.Z + d);
            var b1 = new Vector3(center.X - d, baseY, center.Z + d);
            var b2 = new Vector3(center.X - d, baseY, center.Z - d);
            var b3 = new Vector3(center.X + d, baseY, center.Z - d);

            Line(b0, b1, color); Line(b1, b2, color); Line(b2, b3, color); Line(b3, b0, color);
            Line(b0, tip, color); Line(b1, tip, color); Line(b2, tip, color); Line(b3, tip, color);
        }

        public void PyramidThick(Vector3 center, float size, Color color)
        {
            PyramidWires(center, size, color);
            PyramidWires(center, size * 0.97f, color);
            PyramidWires(center, size * 1.03f, color);
        }

        public void Flush()
        {
            if (_count == 0) return;

            Rlgl.Begin(GlLines);
            for (int i = 0; i < _count; i++)
            {
                int vi = i * 2;
                var c = _colors[vi];
                Rlgl.Color4ub(c.R, c.G, c.B, c.A);
                Rlgl.Vertex3f(_positions[vi].X, _positions[vi].Y, _positions[vi].Z);

                c = _colors[vi + 1];
                Rlgl.Color4ub(c.R, c.G, c.B, c.A);
                Rlgl.Vertex3f(_positions[vi + 1].X, _positions[vi + 1].Y, _positions[vi + 1].Z);
            }
            Rlgl.End();

            _count = 0;
        }
    }
}
