using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class VoxelDeathEffect
    {
        private const float VoxelSize = 0.125f;
        private const float BurstSpeed = 4f;

        private Vector3[] _precomputed = Array.Empty<Vector3>();
        private int _shedIndex;

        public void Precompute(float scale)
        {
            int count = Math.Clamp((int)(20 * scale), 8, 30);
            _precomputed = new Vector3[count];

            float half = scale * 0.3f;
            for (int i = 0; i < count; i++)
            {
                float x = (Random.Shared.NextSingle() * 2f - 1f) * half;
                float y = Random.Shared.NextSingle() * half * 2f;
                float z = (Random.Shared.NextSingle() * 2f - 1f) * half;

                float maxXZ = half * (1f - y / (half * 2f));
                x *= maxXZ / half;
                z *= maxXZ / half;

                _precomputed[i] = new Vector3(x, y, z);
            }

            _shedIndex = 0;
        }

        public void OnDeath(Vector3 pos, Color color)
        {
            var pool = VoxelPool.Instance;
            if (pool == null) return;

            int remaining = Math.Min(8, _precomputed.Length - _shedIndex);
            if (remaining <= 0) remaining = 8;

            for (int i = 0; i < remaining; i++)
            {
                Vector3 offset;
                if (_shedIndex < _precomputed.Length)
                    offset = _precomputed[_shedIndex++];
                else
                    offset = new Vector3(
                        (Random.Shared.NextSingle() * 2f - 1f) * 0.2f,
                        Random.Shared.NextSingle() * 0.4f,
                        (Random.Shared.NextSingle() * 2f - 1f) * 0.2f);

                var spawnPos = pos + offset;

                float u = Random.Shared.NextSingle() * 2f - 1f;
                float theta = Random.Shared.NextSingle() * MathF.Tau;
                float r = MathF.Sqrt(1f - u * u);
                var dir = new Vector3(r * MathF.Cos(theta), MathF.Abs(u), r * MathF.Sin(theta));

                var velocity = dir * (BurstSpeed * (0.5f + Random.Shared.NextSingle()));
                pool.Spawn(spawnPos, velocity, VoxelSize, color, 0f);
            }
        }
    }
}
