using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Grid;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class PathVisualizer
    {
        private static readonly Color BaseLineColor = new(255, 0, 200, 120);
        private static readonly Color PulseColor = new(140, 245, 255, 255);
        private static readonly Color AmbientPulseColor = new(89, 153, 166, 179);

        private const float LineY = 0.15f;
        private const float DotRadius = 0.2f;

        private const float PulseDuration = 0.96f;
        private const float PulseWidth = 0.15f;
        private const float DotFlashScale = 1.8f;

        private const float AmbientPulseInterval = 4f;
        private const float AmbientPulseVariance = 1.5f;

        private readonly List<Vector3[]> _routes = new();
        private readonly List<Vector3[]> _warpedRoutes = new();
        private readonly List<float[]> _segmentNorms = new();

        private float _nextAmbientPulse;
        private float _elapsedTime;

        private float _pulseElapsed;
        private bool _pulsing;
        private Color _activePulseColor;

        public void Init(GridManager gridManager)
        {
            _routes.Clear();
            _warpedRoutes.Clear();
            _segmentNorms.Clear();

            var def = gridManager.Definition;
            foreach (var path in def.Paths)
            {
                var worldPoints = new Vector3[path.Waypoints.Count];
                for (int i = 0; i < path.Waypoints.Count; i++)
                    worldPoints[i] = gridManager.GridToWorld(path.Waypoints[i]);

                _routes.Add(worldPoints);
                _warpedRoutes.Add(new Vector3[worldPoints.Length]);

                var norms = new float[worldPoints.Length];
                float total = 0f;
                for (int i = 1; i < worldPoints.Length; i++)
                    total += Vector3.Distance(worldPoints[i - 1], worldPoints[i]);
                float cumul = 0f;
                norms[0] = 0f;
                for (int i = 1; i < worldPoints.Length; i++)
                {
                    cumul += Vector3.Distance(worldPoints[i - 1], worldPoints[i]);
                    norms[i] = total > 0f ? cumul / total : 0f;
                }
                _segmentNorms.Add(norms);
            }

            ScheduleNextAmbientPulse();
        }

        public void TriggerPulse(Color color)
        {
            _pulsing = true;
            _pulseElapsed = 0f;
            _activePulseColor = color;
        }

        public void Update(float dt)
        {
            _elapsedTime += dt;

            var warp = GridWarpManager.Instance;

            for (int r = 0; r < _routes.Count; r++)
            {
                var src = _routes[r];
                var dst = _warpedRoutes[r];

                for (int i = 0; i < src.Length; i++)
                {
                    float y = LineY;
                    if (warp != null)
                        y += warp.GetWarpOffset(src[i].X, src[i].Z);

                    dst[i] = new Vector3(src[i].X, y, src[i].Z);
                }
            }

            if (_pulsing)
            {
                _pulseElapsed += dt;
                if (_pulseElapsed >= PulseDuration)
                    _pulsing = false;
            }

            if (!_pulsing && _elapsedTime >= _nextAmbientPulse)
            {
                TriggerPulse(AmbientPulseColor);
                ScheduleNextAmbientPulse();
            }
        }

        private void ScheduleNextAmbientPulse()
        {
            _nextAmbientPulse = _elapsedTime + AmbientPulseInterval
                + (Random.Shared.NextSingle() * 2f - 1f) * AmbientPulseVariance;
        }

        public void Render(Camera3D camera)
        {
            float totalT = 1f + PulseWidth;
            float t = _pulsing ? (_pulseElapsed / PulseDuration) * totalT : -1f;

            for (int r = 0; r < _warpedRoutes.Count; r++)
            {
                var points = _warpedRoutes[r];
                if (points.Length < 2) continue;

                var norms = r < _segmentNorms.Count ? _segmentNorms[r] : null;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    float normA = norms != null ? norms[i] : (float)i / (points.Length - 1);
                    float normB = norms != null ? norms[i + 1] : (float)(i + 1) / (points.Length - 1);

                    float pulseA = GetPulseIntensity(normA, t);
                    float pulseB = GetPulseIntensity(normB, t);
                    float pulse = MathF.Max(pulseA, pulseB);

                    var segColor = BlendPulse(BaseLineColor, _pulsing ? _activePulseColor : PulseColor, pulse);
                    Raylib.DrawLine3D(points[i], points[i + 1], segColor);
                }
            }

            for (int r = 0; r < _warpedRoutes.Count; r++)
            {
                var points = _warpedRoutes[r];
                var norms = r < _segmentNorms.Count ? _segmentNorms[r] : null;

                for (int i = 0; i < points.Length; i++)
                {
                    float norm = norms != null ? norms[i] : (float)i / MathF.Max(1, points.Length - 1);
                    float pulse = GetPulseIntensity(norm, t);

                    float scale = DotRadius * MathF.Max(1f, 1f + (DotFlashScale - 1f) * pulse);
                    var dotColor = BlendPulse(BaseLineColor, _pulsing ? _activePulseColor : PulseColor, pulse);

                    WireframeMeshes.DrawSphere(points[i], scale * 0.5f, dotColor);
                }
            }

            Raylib.BeginBlendMode(BlendMode.Additive);
            for (int r = 0; r < _warpedRoutes.Count; r++)
            {
                var points = _warpedRoutes[r];
                var norms = r < _segmentNorms.Count ? _segmentNorms[r] : null;

                for (int i = 0; i < points.Length; i++)
                {
                    float norm = norms != null ? norms[i] : (float)i / MathF.Max(1, points.Length - 1);
                    float pulse = GetPulseIntensity(norm, t);

                    if (pulse > 0.1f)
                    {
                        float glowScale = DotRadius * MathF.Max(1f, 1f + (DotFlashScale - 1f) * pulse) * 0.8f;
                        byte glowA = (byte)Math.Clamp((int)(50 * pulse), 0, 255);
                        WireframeMeshes.DrawSphere(points[i], glowScale,
                            new Color(PulseColor.R, PulseColor.G, PulseColor.B, glowA));
                    }
                }
            }
            Raylib.EndBlendMode();
        }

        private static float GetPulseIntensity(float normalizedPos, float t)
        {
            if (t < 0f) return 0f;

            float dist = MathF.Abs(normalizedPos - t + PulseWidth * 0.5f);
            if (dist > PulseWidth * 0.8f) return 0f;
            return 1f - dist / (PulseWidth * 0.8f);
        }

        private static Color BlendPulse(Color baseCol, Color pulseCol, float t)
        {
            if (t <= 0f) return baseCol;
            t = MathF.Min(t, 1f);
            byte r = (byte)(baseCol.R + (pulseCol.R - baseCol.R) * t);
            byte g = (byte)(baseCol.G + (pulseCol.G - baseCol.G) * t);
            byte b = (byte)(baseCol.B + (pulseCol.B - baseCol.B) * t);
            byte a = (byte)Math.Clamp((int)(baseCol.A + (pulseCol.A - baseCol.A) * t), 0, 255);
            return new Color(r, g, b, a);
        }

        public void Clear()
        {
            _routes.Clear();
            _warpedRoutes.Clear();
            _segmentNorms.Clear();
        }
    }
}
