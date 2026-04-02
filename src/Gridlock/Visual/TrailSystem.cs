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
        private const int MaxPointsMask = MaxPoints - 1;

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
                int lastIdx = (_heads[trailId] + _counts[trailId] - 1) & MaxPointsMask;
                float distSq = Vector3.DistanceSquared(position, _points[trailId][lastIdx].Position);
                if (distSq < 0.006f) return;
            }

            int newIdx = (_heads[trailId] + _counts[trailId]) & MaxPointsMask;
            if (_counts[trailId] >= MaxPoints)
                _heads[trailId] = (_heads[trailId] + 1) & MaxPointsMask;
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
                    int idx = (head + i) & MaxPointsMask;
                    _points[t][idx].Time += dt;

                    if (_points[t][idx].Time >= duration)
                        removed++;
                    else
                        break;
                }

                if (removed > 0)
                {
                    _heads[t] = (head + removed) & MaxPointsMask;
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
                float invDuration = 1f / duration;
                int head = _heads[t];
                int count = _counts[t];
                var startC = _startColors[t];
                var midC = _midColors[t];
                var endC = _endColors[t];

                for (int i = 0; i < count - 1; i++)
                {
                    int idxA = (head + i) & MaxPointsMask;
                    int idxB = (head + i + 1) & MaxPointsMask;

                    ref var a = ref _points[t][idxA];
                    ref var b = ref _points[t][idxB];

                    float tA = a.Time * invDuration;
                    float tB = b.Time * invDuration;
                    if (tA > 1f) tA = 1f;
                    if (tB > 1f) tB = 1f;

                    float midFade = 1f - (tA + tB) * 0.5f;
                    float tMid = (tA + tB) * 0.5f;

                    byte r = GradientByte(startC.R, midC.R, endC.R, tMid);
                    byte g = GradientByte(startC.G, midC.G, endC.G, tMid);
                    byte bVal = GradientByte(startC.B, midC.B, endC.B, tMid);
                    byte alpha = (byte)(200f * midFade);
                    if (alpha < 2) continue;

                    Rendering.LineBatch.ThickLine3D(a.Position, b.Position, new Color(r, g, bVal, alpha));
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
