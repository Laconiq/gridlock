using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Grid;
using Gridlock.Loot;

namespace Gridlock.Enemies
{
    public sealed class EnemySpawner
    {
        public event Action? OnEnemyDespawned;
        public event Action? OnSpawningComplete;
        public event Action<Vector3>? OnEnemyKilled;

        private readonly GridManager _gridManager;
        private int _nextSpawnIndex;

        private readonly List<Enemy> _activeEnemies = new();
        private readonly List<Enemy> _removalBuffer = new();

        private WaveDefinition? _currentWave;
        private int _entryIndex;
        private int _spawnedInGroup;
        private float _groupDelayTimer;
        private float _spawnTimer;
        private bool _spawning;

        public IReadOnlyList<Enemy> ActiveEnemies => _activeEnemies;

        public EnemySpawner(GridManager gridManager)
        {
            _gridManager = gridManager;
        }

        private Vector3 GetSpawnPosition()
        {
            var spawns = _gridManager.SpawnPositions;
            if (spawns.Count == 0) return Vector3.Zero;

            var pos = spawns[_nextSpawnIndex % spawns.Count];
            _nextSpawnIndex++;
            return pos;
        }

        public void SpawnWave(WaveDefinition wave)
        {
            _currentWave = wave;
            _entryIndex = 0;
            _spawnedInGroup = 0;
            _spawning = true;

            if (wave.Entries.Count > 0)
                _groupDelayTimer = wave.Entries[0].DelayBeforeGroup;
            else
                FinishSpawning();
        }

        public void Update(float dt)
        {
            UpdateSpawning(dt);
            UpdateEnemies(dt);
            ProcessRemovals();
        }

        private void UpdateSpawning(float dt)
        {
            if (!_spawning || _currentWave == null) return;

            if (_entryIndex >= _currentWave.Entries.Count)
            {
                FinishSpawning();
                return;
            }

            var entry = _currentWave.Entries[_entryIndex];

            if (_groupDelayTimer > 0f)
            {
                _groupDelayTimer -= dt;
                return;
            }

            _spawnTimer -= dt;
            if (_spawnTimer > 0f) return;

            SpawnEnemy(entry.Enemy, tracked: true);
            _spawnedInGroup++;
            _spawnTimer = entry.SpawnInterval;

            if (_spawnedInGroup >= entry.Count)
            {
                _entryIndex++;
                _spawnedInGroup = 0;
                _spawnTimer = 0f;

                if (_entryIndex < _currentWave.Entries.Count)
                    _groupDelayTimer = _currentWave.Entries[_entryIndex].DelayBeforeGroup;
            }
        }

        private void FinishSpawning()
        {
            _spawning = false;
            OnSpawningComplete?.Invoke();
        }

        private void SpawnEnemy(EnemyData definition, bool tracked)
        {
            var pos = GetSpawnPosition();
            pos.Y = 0.5f;

            var enemy = new Enemy(definition, pos);

            var route = _gridManager.GetRoute(0);
            if (route != null)
            {
                int nearest = _gridManager.GetNearestWaypointIndex(0, pos);
                if (nearest < route.Length)
                {
                    float distToNearest = Vector3.Distance(pos, route[nearest]);
                    if (distToNearest < 0.3f && nearest < route.Length - 1)
                        nearest++;
                }
                enemy.AssignRoute(route, nearest);
            }

            EnemyRegistry.Register(enemy);
            _activeEnemies.Add(enemy);

            if (tracked)
            {
                bool despawned = false;
                void NotifyDespawn(Enemy _)
                {
                    if (despawned) return;
                    despawned = true;
                    OnEnemyDespawned?.Invoke();
                }
                enemy.OnReachedObjective += NotifyDespawn;
                enemy.Health.OnDeath += () =>
                {
                    OnEnemyKilled?.Invoke(enemy.Position);
                    if (despawned) return;
                    despawned = true;
                    OnEnemyDespawned?.Invoke();
                    LootDropper.Instance?.OnEnemyDied(enemy.Position);
                };
            }
        }

        private void UpdateEnemies(float dt)
        {
            for (int i = 0; i < _activeEnemies.Count; i++)
                _activeEnemies[i].Update(dt);
        }

        private void ProcessRemovals()
        {
            _removalBuffer.Clear();
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                var enemy = _activeEnemies[i];
                if (enemy.Health.PendingRemoval)
                    _removalBuffer.Add(enemy);
            }

            foreach (var enemy in _removalBuffer)
            {
                _activeEnemies.Remove(enemy);
                EnemyRegistry.Unregister(enemy);
            }
        }

        public void Clear()
        {
            foreach (var enemy in _activeEnemies)
                EnemyRegistry.Unregister(enemy);

            _activeEnemies.Clear();
            _spawning = false;
            _currentWave = null;
        }
    }
}
