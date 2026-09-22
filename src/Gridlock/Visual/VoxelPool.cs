using System;
using System.Numerics;
using Gridlock.Grid;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class VoxelPool
    {
        public static VoxelPool? Instance { get; private set; }

        private const int MaxVoxels = 4096;
        private const float Gravity = 9.81f;
        private const float BounceYDamping = -0.25f;
        private const float BounceXZDamping = 0.6f;
        private const float SettleAge = 2f;
        private const float MaxAge = 4f;

        private readonly Vector3[] _positions = new Vector3[MaxVoxels];
        private readonly Vector3[] _velocities = new Vector3[MaxVoxels];
        private readonly float[] _sizes = new float[MaxVoxels];
        private readonly float[] _ages = new float[MaxVoxels];
        private readonly float[] _floorYs = new float[MaxVoxels];
        private readonly Color[] _colors = new Color[MaxVoxels];
        private int _count;

        public void Init() { Instance = this; }
        public void Shutdown() { Instance = null; }

        public void Spawn(Vector3 pos, Vector3 velocity, float size, Color color, float floorY)
        {
            if (_count >= MaxVoxels)
                RecycleOldest();

            ref var i = ref _count;
            _positions[i] = pos;
            _velocities[i] = velocity;
            _sizes[i] = size;
            _ages[i] = 0f;
            _floorYs[i] = floorY;
            _colors[i] = color;
            i++;
        }

        public void Update(float dt)
        {
            var warp = GridWarpManager.Instance;

            for (int i = _count - 1; i >= 0; i--)
            {
                _ages[i] += dt;

                if (_ages[i] > MaxAge)
                {
                    RemoveAt(i);
                    continue;
                }

                if (_ages[i] >= SettleAge)
                {
                    if (warp != null)
                        _positions[i].Y = _floorYs[i] + warp.GetWarpOffset(_positions[i].X, _positions[i].Z);
                    continue;
                }

                _velocities[i].Y -= Gravity * dt;
                _positions[i] += _velocities[i] * dt;

                float floor = _floorYs[i];
                if (warp != null)
                    floor += warp.GetWarpOffset(_positions[i].X, _positions[i].Z);

                if (_positions[i].Y <= floor)
                {
                    _positions[i].Y = floor;
                    _velocities[i].Y *= BounceYDamping;
                    _velocities[i].X *= BounceXZDamping;
                    _velocities[i].Z *= BounceXZDamping;
                }
            }
        }

        public void Render()
        {
            for (int i = 0; i < _count; i++)
            {
                float age = _ages[i];
                float fade = age < SettleAge ? 1f : 1f - Math.Clamp((age - SettleAge) / (MaxAge - SettleAge), 0f, 1f);
                float s = _sizes[i];

                var drawPos = new System.Numerics.Vector3(_positions[i].X, _positions[i].Y, _positions[i].Z);

                byte a = (byte)Math.Clamp((int)(255f * fade), 0, 255);
                var color = new Color(_colors[i].R, _colors[i].G, _colors[i].B, a);
                Raylib.DrawCube(drawPos, s, s, s, color);
            }

            Raylib.BeginBlendMode(BlendMode.Additive);
            for (int i = 0; i < _count; i++)
            {
                float age = _ages[i];
                float fade = age < SettleAge ? 1f : 1f - Math.Clamp((age - SettleAge) / (MaxAge - SettleAge), 0f, 1f);
                float freshGlow = MathF.Max(0f, 1f - age * 2f);
                float glowFade = fade * (0.3f + 0.7f * freshGlow);

                if (glowFade < 0.05f) continue;

                float s = _sizes[i] * 1.6f;
                var drawPos = new System.Numerics.Vector3(_positions[i].X, _positions[i].Y, _positions[i].Z);
                byte ga = (byte)Math.Clamp((int)(60f * glowFade), 0, 255);
                Raylib.DrawCube(drawPos, s, s, s,
                    new Color(_colors[i].R, _colors[i].G, _colors[i].B, ga));
            }
            Raylib.EndBlendMode();
        }

        public void Clear() { _count = 0; }

        private void RecycleOldest()
        {
            float oldest = 0f;
            int idx = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_ages[i] > oldest)
                {
                    oldest = _ages[i];
                    idx = i;
                }
            }
            RemoveAt(idx);
        }

        private void RemoveAt(int idx)
        {
            int last = _count - 1;
            if (idx < last)
            {
                _positions[idx] = _positions[last];
                _velocities[idx] = _velocities[last];
                _sizes[idx] = _sizes[last];
                _ages[idx] = _ages[last];
                _floorYs[idx] = _floorYs[last];
                _colors[idx] = _colors[last];
            }
            _count--;
        }
    }
}
