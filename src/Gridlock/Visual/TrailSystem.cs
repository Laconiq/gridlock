using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public struct TrailPoint
    {
        public Vector3 Position;
        public float Time;
    }

    public sealed class TrailSystem
    {
        public static TrailSystem Instance { get; private set; } = new();

        private const int MaxTrails = 256;
        private const int MaxPoints = 64;

        private readonly TrailPoint[][] _points;
        private readonly int[] _heads;
        private readonly int[] _counts;
        private readonly float[] _durations;
        private readonly float[] _widths;
        private readonly Color[] _startColors;
        private readonly Color[] _midColors;
        private readonly Color[] _endColors;
        private readonly bool[] _alive;
        private int _nextId;

        public TrailSystem()
        {
            _points = new TrailPoint[MaxTrails][];
            _heads = new int[MaxTrails];
            _counts = new int[MaxTrails];
            _durations = new float[MaxTrails];
            _widths = new float[MaxTrails];
            _startColors = new Color[MaxTrails];
            _midColors = new Color[MaxTrails];
            _endColors = new Color[MaxTrails];
            _alive = new bool[MaxTrails];

            for (int i = 0; i < MaxTrails; i++)
                _points[i] = new TrailPoint[MaxPoints];

            Instance = this;
        }

        public int CreateTrail(float duration, float width, Color startColor, Color endColor, Color? midColor = null)
        {
            int slot = -1;
            for (int i = 0; i < MaxTrails; i++)
            {
                int idx = (_nextId + i) % MaxTrails;
                if (!_alive[idx])
                {
                    slot = idx;
                    break;
                }
            }

            if (slot == -1)
                slot = _nextId % MaxTrails;

            _alive[slot] = true;
            _heads[slot] = 0;
            _counts[slot] = 0;
            _durations[slot] = duration;
            _widths[slot] = MathF.Min(width, 0.35f);
            _startColors[slot] = startColor;
            _midColors[slot] = midColor ?? startColor;
            _endColors[slot] = endColor;
            _nextId = (slot + 1) % MaxTrails;

            return slot;
        }

        public void AddPoint(int trailId, Vector3 position)
        {
            if (trailId < 0 || trailId >= MaxTrails || !_alive[trailId]) return;

            if (_counts[trailId] > 0)
            {
                int lastIdx = (_heads[trailId] + _counts[trailId] - 1) % MaxPoints;
                float distSq = Vector3.DistanceSquared(position, _points[trailId][lastIdx].Position);
                if (distSq < 0.006f) return;
            }

            int newIdx = (_heads[trailId] + _counts[trailId]) % MaxPoints;
            if (_counts[trailId] >= MaxPoints)
                _heads[trailId] = (_heads[trailId] + 1) % MaxPoints;
            else
                _counts[trailId]++;

            _points[trailId][newIdx] = new TrailPoint
            {
                Position = position,
                Time = 0f,
            };
        }

        public void DestroyTrail(int trailId)
        {
            if (trailId < 0 || trailId >= MaxTrails) return;
            _alive[trailId] = false;
            _counts[trailId] = 0;
        }

        public void Update(float dt)
        {
            for (int t = 0; t < MaxTrails; t++)
            {
                if (!_alive[t] || _counts[t] == 0) continue;

                float duration = _durations[t];
                int head = _heads[t];
                int count = _counts[t];
                int removed = 0;

                for (int i = 0; i < count; i++)
                {
                    int idx = (head + i) % MaxPoints;
                    _points[t][idx].Time += dt;

                    if (_points[t][idx].Time >= duration)
                        removed++;
                    else
                        break;
                }

                if (removed > 0)
                {
                    _heads[t] = (head + removed) % MaxPoints;
                    _counts[t] -= removed;
                }
            }
        }

        public void Render(Camera3D camera)
        {
            Raylib.BeginBlendMode(BlendMode.Additive);

            for (int t = 0; t < MaxTrails; t++)
            {
                if (!_alive[t] || _counts[t] < 2) continue;

                float duration = _durations[t];
                int head = _heads[t];
                int count = _counts[t];

                for (int i = 0; i < count - 1; i++)
                {
                    int idxA = (head + i) % MaxPoints;
                    int idxB = (head + i + 1) % MaxPoints;

                    ref var a = ref _points[t][idxA];
                    ref var b = ref _points[t][idxB];

                    float tA = Math.Clamp(a.Time / duration, 0f, 1f);
                    float tB = Math.Clamp(b.Time / duration, 0f, 1f);

                    float fadeA = 1f - tA;
                    float fadeB = 1f - tB;

                    float midFade = (fadeA + fadeB) * 0.5f;

                    float tMid = (tA + tB) * 0.5f;
                    byte r = GradientByte(_startColors[t].R, _midColors[t].R, _endColors[t].R, tMid);
                    byte g = GradientByte(_startColors[t].G, _midColors[t].G, _endColors[t].G, tMid);
                    byte bVal = GradientByte(_startColors[t].B, _midColors[t].B, _endColors[t].B, tMid);
                    byte alpha = (byte)Math.Clamp((int)(200f * midFade), 0, 255);

                    var color = new Color(r, g, bVal, alpha);
                    Raylib.DrawLine3D(a.Position, b.Position, color);
                }
            }

            Raylib.EndBlendMode();
        }

        public void Clear()
        {
            for (int i = 0; i < MaxTrails; i++)
            {
                _alive[i] = false;
                _counts[i] = 0;
            }
        }

        private static byte LerpByte(byte a, byte b, float t)
        {
            return (byte)(a + (b - a) * t);
        }

        private static byte GradientByte(byte start, byte mid, byte end, float t)
        {
            const float midPoint = 0.05f;
            if (t <= midPoint)
            {
                float local = t / midPoint;
                return LerpByte(start, mid, local);
            }
            else
            {
                float local = (t - midPoint) / (1f - midPoint);
                return LerpByte(mid, end, local);
            }
        }
    }
}
