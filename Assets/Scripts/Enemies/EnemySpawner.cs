using System.Collections;
using System.Linq;
using AIWE.Combat;
using AIWE.LevelDesign;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemySpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Vector3 targetPosition = new(0, 0, -20);

        [Header("Test Mode")]
        [SerializeField] private bool testMode;
        [SerializeField] private EnemyDefinition testEnemy;
        [SerializeField] private float testInterval = 5f;

        private Transform[] _spawnPoints;
        private int _nextSpawnIndex;
        private float _testTimer;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            if (spawnPoint == null)
                FindLDtkSpawnPoints();

            FindLDtkObjective();
        }

        private void FindLDtkSpawnPoints()
        {
            var markers = FindObjectsByType<EnemySpawnerMarker>(FindObjectsSortMode.None);
            if (markers.Length > 0)
            {
                _spawnPoints = markers.Select(m => m.transform).ToArray();
                spawnPoint = _spawnPoints[0];
                Debug.Log($"[EnemySpawner] Found {_spawnPoints.Length} LDtk spawn points");
            }
        }

        private void FindLDtkObjective()
        {
            var objective = FindAnyObjectByType<ObjectiveMarker>();
            if (objective != null)
            {
                targetPosition = objective.transform.position;
                Debug.Log($"[EnemySpawner] Target set to LDtk Objective at {targetPosition}");
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

            var state = Core.GameManager.Instance?.CurrentState.Value;
            if (state != Core.GameState.Wave) return;

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
                    SpawnEnemy(entry.enemy);
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }
        }

        private void SpawnEnemy(EnemyDefinition definition)
        {
            if (enemyPrefab == null || !IsServer) return;

            var pos = GetSpawnPosition();
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            var controller = go.GetComponent<EnemyController>();
            controller?.Setup(definition, targetPosition);

            var health = go.GetComponent<EnemyHealth>();
            health?.SetMaxHP(definition.maxHP);

            if (go.GetComponent<StatusEffectManager>() == null)
                go.AddComponent<StatusEffectManager>();
        }
    }
}
