using System.Collections.Generic;
using System.Linq;
using AIWE.Enemies;
using AIWE.HUD;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Core
{
    public class WaveManager : NetworkBehaviour
    {
        [SerializeField] private List<WaveDefinition> waves = new();
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private float waveClearedDuration = 2f;

        private readonly NetworkVariable<int> _currentWave = new(0);
        private readonly NetworkVariable<int> _enemiesRemaining = new(0);
        private int _aliveCount;
        private bool _spawningComplete;

        public int CurrentWave => _currentWave.Value;
        public int EnemiesRemaining => _enemiesRemaining.Value;
        public int TotalWaves => waves != null ? waves.Count : 0;

        public void ResetWaves()
        {
            if (!IsServer) return;
            _currentWave.Value = 0;
            _aliveCount = 0;
            _spawningComplete = false;
            _enemiesRemaining.Value = 0;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            var gm = GameManager.Instance;
            if (gm != null)
                gm.CurrentState.OnValueChanged += OnGameStateChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;
            var gm = GameManager.Instance;
            if (gm != null)
                gm.CurrentState.OnValueChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (!IsServer) return;

            if (current == GameState.Wave)
                StartWave();
        }

        private void StartWave()
        {
            if (spawner == null || waves.Count == 0) return;

            var waveIndex = _currentWave.Value % waves.Count;
            var wave = waves[waveIndex];
            if (wave.entries == null || wave.entries.Count == 0) return;
            var total = wave.entries.Sum(e => e.count);

            _aliveCount = total;
            _spawningComplete = false;
            _enemiesRemaining.Value = total;

            spawner.OnEnemyDespawned += HandleEnemyDespawned;
            spawner.OnSpawningComplete += HandleSpawningComplete;

            spawner.SpawnWave(wave);
            Debug.Log($"[WaveManager] Wave {_currentWave.Value + 1}: {total} enemies");
        }

        private void HandleEnemyDespawned()
        {
            _aliveCount--;
            _enemiesRemaining.Value = _aliveCount;
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

            var clearedWaveNumber = _currentWave.Value + 1;
            _currentWave.Value++;
            ShowWaveClearedClientRpc(clearedWaveNumber);
            GameManager.Instance?.SetState(GameState.Preparing);
            Debug.Log("[WaveManager] Wave complete");
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ShowWaveClearedClientRpc(int waveNumber)
        {
            GameHUD.Instance?.ShowAnnouncement($"WAVE_{waveNumber:D2}_CLEARED", waveClearedDuration);
        }
    }
}
