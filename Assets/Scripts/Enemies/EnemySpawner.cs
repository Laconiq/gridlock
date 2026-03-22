using System;
using System.Collections;
using System.Linq;
using AIWE.AI;
using AIWE.Core;
using AIWE.LevelDesign;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemySpawner : NetworkBehaviour
    {
        public event Action OnEnemyDespawned;
        public event Action OnSpawningComplete;

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform spawnPoint;

        [Header("Spawn")]
        [SerializeField] private float minSpawnHeight = 1.25f;

        [Header("Test Mode")]
        [SerializeField] private bool testMode;
        [SerializeField] private EnemyDefinition testEnemy;
        [SerializeField] private float testInterval = 5f;

        private Transform[] _spawnPoints;
        private int _nextSpawnIndex;
        private float _testTimer;
        private RouteManager _routeManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            if (spawnPoint == null)
                FindLDtkSpawnPoints();

            _routeManager = ServiceLocator.Get<RouteManager>();
        }

        private void FindLDtkSpawnPoints()
        {
            var markers = FindObjectsByType<EnemySpawnerMarker>();
            if (markers.Length > 0)
            {
                _spawnPoints = markers.Select(m => m.transform).ToArray();
                spawnPoint = _spawnPoints[0];
                Debug.Log($"[EnemySpawner] Found {_spawnPoints.Length} LDtk spawn points");
            }
        }

        private Vector3 GetSpawnPosition()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var pos = _spawnPoints[_nextSpawnIndex].position;
                _nextSpawnIndex = (_nextSpawnIndex + 1) % _spawnPoints.Length;
                return pos;
            }
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private void Update()
        {
            if (!testMode || !IsServer || testEnemy == null) return;

            var state = GameManager.Instance?.CurrentState.Value;
            if (state != GameState.Wave) return;

            _testTimer -= Time.deltaTime;
            if (_testTimer <= 0f)
            {
                _testTimer = testInterval;
                SpawnEnemy(testEnemy);
            }
        }

        public void SpawnWave(WaveDefinition wave)
        {
            if (!IsServer) return;
            StartCoroutine(SpawnWaveCoroutine(wave));
        }

        private IEnumerator SpawnWaveCoroutine(WaveDefinition wave)
        {
            foreach (var entry in wave.entries)
            {
                if (entry.delayBeforeGroup > 0)
                    yield return new WaitForSeconds(entry.delayBeforeGroup);

                for (int i = 0; i < entry.count; i++)
                {
                    SpawnEnemy(entry.enemy, tracked: true);
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }

            OnSpawningComplete?.Invoke();
        }

        private void SpawnEnemy(EnemyDefinition definition, bool tracked = false, int routeId = 0)
        {
            if (enemyPrefab == null || !IsServer) return;

            var pos = GetSpawnPosition();
            pos.y = Mathf.Max(pos.y, minSpawnHeight);
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            var controller = go.GetComponent<EnemyController>();
            controller?.Setup(definition);

            var health = go.GetComponent<EnemyHealth>();
            health?.SetMaxHP(definition.maxHP);

            var ai = go.GetComponent<EnemyAI>();
            ai?.Setup(routeId, definition);

            if (tracked)
            {
                if (controller != null) controller.OnReachedObjective += () => OnEnemyDespawned?.Invoke();
                if (health != null) health.OnDeath += () => OnEnemyDespawned?.Invoke();
            }
        }
    }
}
