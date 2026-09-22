using System;
using System.Collections.Generic;
using System.Linq;
using Gridlock.Core;

namespace Gridlock.Enemies
{
    public sealed class WaveManager
    {
        private readonly List<WaveDefinition> _waves;
        private readonly EnemySpawner _spawner;
        private readonly float _waveClearedDuration;

        private int _currentWave;
        private int _aliveCount;
        private bool _spawningComplete;
        private int _enemiesRemaining;

        public int CurrentWave => _currentWave;
        public int EnemiesRemaining => _enemiesRemaining;
        public int TotalWaves => _waves.Count;

        public event Action<int>? OnWaveCleared;

        public WaveManager(List<WaveDefinition> waves, EnemySpawner spawner, float waveClearedDuration = 2f)
        {
            _waves = waves;
            _spawner = spawner;
            _waveClearedDuration = waveClearedDuration;

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged += OnGameStateChanged;
        }

        public void ResetWaves()
        {
            _currentWave = 0;
            _aliveCount = 0;
            _spawningComplete = false;
            _enemiesRemaining = 0;
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (current == GameState.Wave)
                StartWave();
        }

        private void StartWave()
        {
            if (_waves.Count == 0) return;

            int waveIndex = _currentWave % _waves.Count;
            var wave = _waves[waveIndex];
            if (wave.Entries.Count == 0)
            {
                Console.WriteLine($"[WaveManager] Wave {_currentWave} has no entries");
                GameManager.Instance?.SetState(GameState.Preparing);
                return;
            }

            int total = wave.Entries.Sum(e => e.Count);
            _aliveCount = total;
            _spawningComplete = false;
            _enemiesRemaining = total;

            _spawner.OnEnemyDespawned -= HandleEnemyDespawned;
            _spawner.OnSpawningComplete -= HandleSpawningComplete;
            _spawner.OnEnemyDespawned += HandleEnemyDespawned;
            _spawner.OnSpawningComplete += HandleSpawningComplete;

            _spawner.SpawnWave(wave);
            Console.WriteLine($"[WaveManager] Wave {_currentWave + 1}: {total} enemies");
        }

        private void HandleEnemyDespawned()
        {
            if (_aliveCount <= 0) return;
            _aliveCount--;
            _enemiesRemaining = _aliveCount;
            CheckWaveComplete();
        }

        private void HandleSpawningComplete()
        {
            _spawningComplete = true;
            CheckWaveComplete();
        }

        private void CheckWaveComplete()
        {
            if (!_spawningComplete || _aliveCount > 0) return;

            _spawner.OnEnemyDespawned -= HandleEnemyDespawned;
            _spawner.OnSpawningComplete -= HandleSpawningComplete;

            int clearedWaveNumber = _currentWave + 1;
            _currentWave++;

            OnWaveCleared?.Invoke(clearedWaveNumber);
            GameManager.Instance?.SetState(GameState.Preparing);
            Console.WriteLine("[WaveManager] Wave complete");
        }

        public void Shutdown()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnGameStateChanged;
        }
    }
}
