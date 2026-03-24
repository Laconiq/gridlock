using System;
using System.Collections;
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

        private EnemySpawnerMarker[] _spawnPoints;
        private int _nextSpawnIndex;
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
                _spawnPoints = markers;
                spawnPoint = _spawnPoints[0].transform;
                Debug.Log($"[EnemySpawner] Found {_spawnPoints.Length} LDtk spawn points");
            }
        }

        private EnemySpawnerMarker GetNextSpawnMarker()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var marker = _spawnPoints[_nextSpawnIndex];
                _nextSpawnIndex = (_nextSpawnIndex + 1) % _spawnPoints.Length;
                return marker;
            }
            return null;
        }

        private Vector3 GetSpawnPosition(EnemySpawnerMarker marker)
        {
            if (marker != null) return marker.transform.position;
            return spawnPoint != null ? spawnPoint.position : transform.position;
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
                    var marker = GetNextSpawnMarker();
                    SpawnEnemy(entry.enemy, tracked: true, marker: marker);
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }

            OnSpawningComplete?.Invoke();
        }

        private void SpawnEnemy(EnemyDefinition definition, bool tracked = false, EnemySpawnerMarker marker = null)
        {
            if (enemyPrefab == null || !IsServer) return;

            int routeId = marker != null ? marker.WaveGroup : 0;
            var pos = GetSpawnPosition(marker);
            pos.y = Mathf.Max(pos.y, minSpawnHeight);
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            var controller = go.GetComponent<EnemyController>();
            controller?.Setup(definition);

            var health = go.GetComponent<EnemyHealth>();
            health?.SetInitialHP(definition.maxHP);

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
