using System.Collections.Generic;
using System.Linq;
using Gridlock.Enemies;
using Gridlock.HUD;
using UnityEngine;

namespace Gridlock.Core
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private List<WaveDefinition> waves = new();
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private float waveClearedDuration = 2f;

        private int _currentWave;
        private int _enemiesRemaining;
        private int _aliveCount;
        private bool _spawningComplete;

        public int CurrentWave => _currentWave;
        public int EnemiesRemaining => _enemiesRemaining;
        public int TotalWaves => waves != null ? waves.Count : 0;

        public void ResetWaves()
        {
            _currentWave = 0;
            _aliveCount = 0;
            _spawningComplete = false;
            _enemiesRemaining = 0;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnGameStateChanged;

            if (spawner != null)
            {
                spawner.OnEnemyDespawned -= HandleEnemyDespawned;
                spawner.OnSpawningComplete -= HandleSpawningComplete;
            }
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (current == GameState.Wave)
                StartWave();
        }

        private void StartWave()
        {
            if (spawner == null || waves.Count == 0) return;

            var waveIndex = _currentWave % waves.Count;
            var wave = waves[waveIndex];
            if (wave.entries == null || wave.entries.Count == 0)
            {
                Debug.LogError($"[WaveManager] Wave {_currentWave} has no entries — returning to Preparing");
                GameManager.Instance?.SetState(GameState.Preparing);
                return;
            }
            var total = wave.entries.Sum(e => e.count);

            _aliveCount = total;
            _spawningComplete = false;
            _enemiesRemaining = total;

            spawner.OnEnemyDespawned -= HandleEnemyDespawned;
            spawner.OnSpawningComplete -= HandleSpawningComplete;
            spawner.OnEnemyDespawned += HandleEnemyDespawned;
            spawner.OnSpawningComplete += HandleSpawningComplete;

            spawner.SpawnWave(wave);
            Debug.Log($"[WaveManager] Wave {_currentWave + 1}: {total} enemies");
        }

        private void HandleEnemyDespawned()
        {
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

            spawner.OnEnemyDespawned -= HandleEnemyDespawned;
            spawner.OnSpawningComplete -= HandleSpawningComplete;

            var clearedWaveNumber = _currentWave + 1;
            _currentWave++;
            GameHUD.Instance?.ShowAnnouncement($"WAVE_{clearedWaveNumber:D2}_CLEARED", waveClearedDuration);
            GameManager.Instance?.SetState(GameState.Preparing);
            Debug.Log("[WaveManager] Wave complete");
        }
    }
}
