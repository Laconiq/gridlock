using System.Collections;
using AIWE.Combat;
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

        private float _testTimer;

        private void Update()
        {
            if (!testMode || !IsServer || testEnemy == null) return;

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

            var pos = spawnPoint != null ? spawnPoint.position : transform.position;
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);

            var netObj = go.GetComponent<NetworkObject>();
            netObj.Spawn();

            var controller = go.GetComponent<EnemyController>();
            controller?.Setup(definition, targetPosition);

            var health = go.GetComponent<EnemyHealth>();
            health?.SetMaxHP(definition.maxHP);

            if (go.GetComponent<StatusEffectManager>() == null)
                go.AddComponent<StatusEffectManager>();
        }
    }
}
