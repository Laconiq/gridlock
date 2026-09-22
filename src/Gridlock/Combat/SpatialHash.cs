using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Enemies;

namespace Gridlock.Combat
{
    public sealed class SpatialHash
    {
        private readonly float _invCellSize;
        private readonly Dictionary<long, List<Enemy>> _cells = new(128);
        private readonly Stack<List<Enemy>> _listPool = new(64);

        public SpatialHash(float cellSize = 2.0f)
        {
            _invCellSize = 1f / cellSize;
        }

        public void Clear()
        {
            foreach (var kv in _cells)
            {
                kv.Value.Clear();
                _listPool.Push(kv.Value);
            }
            _cells.Clear();
        }

        public void Insert(Enemy enemy)
        {
            long key = CellKey(enemy.Position.X, enemy.Position.Z);
            if (!_cells.TryGetValue(key, out var list))
            {
                list = _listPool.TryPop(out var recycled) ? recycled : new List<Enemy>(8);
                _cells[key] = list;
            }
            list.Add(enemy);
        }

        public void Rebuild(IReadOnlyList<Enemy> enemies)
        {
            Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e.IsAlive)
                    Insert(e);
            }
        }

        public void QuerySegment(Vector3 from, Vector3 to, float radius, List<Enemy> results)
        {
            float minX = MathF.Min(from.X, to.X) - radius;
            float maxX = MathF.Max(from.X, to.X) + radius;
            float minZ = MathF.Min(from.Z, to.Z) - radius;
            float maxZ = MathF.Max(from.Z, to.Z) + radius;

            int x0 = (int)MathF.Floor(minX * _invCellSize);
            int x1 = (int)MathF.Floor(maxX * _invCellSize);
            int z0 = (int)MathF.Floor(minZ * _invCellSize);
            int z1 = (int)MathF.Floor(maxZ * _invCellSize);

            for (int gx = x0; gx <= x1; gx++)
            {
                for (int gz = z0; gz <= z1; gz++)
                {
                    long key = PackKey(gx, gz);
                    if (_cells.TryGetValue(key, out var list))
                    {
                        for (int i = 0; i < list.Count; i++)
                            results.Add(list[i]);
                    }
                }
            }
        }

        private long CellKey(float x, float z)
        {
            int gx = (int)MathF.Floor(x * _invCellSize);
            int gz = (int)MathF.Floor(z * _invCellSize);
            return PackKey(gx, gz);
        }

        private static long PackKey(int x, int z)
        {
            return ((long)x << 32) | (uint)z;
        }
    }
}
