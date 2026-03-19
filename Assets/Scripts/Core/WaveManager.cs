using System.Collections.Generic;
using AIWE.Enemies;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Core
{
    public class WaveManager : NetworkBehaviour
    {
        [SerializeField] private List<WaveDefinition> waves = new();
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private float intermissionDuration = 5f;

        private readonly NetworkVariable<int> _currentWave = new(0);
        private float _intermissionTimer;
        private bool _waveActive;

        public int CurrentWave => _currentWave.Value;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.CurrentState.OnValueChanged += OnGameStateChanged;
            }
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
            {
                StartWave();
            }
        }

        private void StartWave()
        {
            if (spawner == null || waves.Count == 0) return;

            var waveIndex = _currentWave.Value % waves.Count;
            spawner.SpawnWave(waves[waveIndex]);
            _waveActive = true;
            Debug.Log($"[WaveManager] Starting wave {_currentWave.Value + 1}");
        }

        public void OnWaveComplete()
        {
            if (!IsServer) return;
            _waveActive = false;
            _currentWave.Value++;
            GameManager.Instance?.SetState(GameState.Intermission);
            _intermissionTimer = intermissionDuration;
        }

        private void Update()
        {
            if (!IsServer) return;

            if (GameManager.Instance?.CurrentState.Value == GameState.Intermission)
            {
                _intermissionTimer -= Time.deltaTime;
                if (_intermissionTimer <= 0f)
                {
                    GameManager.Instance.SetState(GameState.Wave);
                }
            }
        }
    }
}
