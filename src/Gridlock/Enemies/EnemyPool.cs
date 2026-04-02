using System.Collections.Generic;
using System.Numerics;

namespace Gridlock.Enemies
{
    public sealed class EnemyPool
    {
        private readonly Stack<Enemy> _pool = new(64);

        public Enemy Rent(EnemyData data, Vector3 spawnPos)
        {
            if (_pool.TryPop(out var enemy))
            {
                enemy.Reset(data, spawnPos);
                return enemy;
            }
            return new Enemy(data, spawnPos);
        }

        public void Return(Enemy enemy)
        {
            _pool.Push(enemy);
        }

        public void Clear()
        {
            _pool.Clear();
        }
    }
}
