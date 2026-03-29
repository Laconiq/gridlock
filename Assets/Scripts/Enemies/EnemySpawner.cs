using System;
using System.Collections;
using Gridlock.AI;
using Gridlock.Core;
using Gridlock.Grid;
using UnityEngine;

namespace Gridlock.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        public event Action OnEnemyDespawned;
        public event Action OnSpawningComplete;

        [SerializeField] private GameObject enemyPrefab;

        private GridManager _gridManager;
        private RouteManager _routeManager;
        private int _nextSpawnIndex;

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _routeManager = ServiceLocator.Get<RouteManager>();
        }

        private Vector3 GetSpawnPosition()
        {
            if (_gridManager == null || _gridManager.SpawnPositions.Count == 0)
                return transform.position;

            var pos = _gridManager.SpawnPositions[_nextSpawnIndex % _gridManager.SpawnPositions.Count];
            _nextSpawnIndex++;
            return pos;
        }

        public void SpawnWave(WaveDefinition wave)
        {
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

        private void SpawnEnemy(EnemyDefinition definition, bool tracked = false)
        {
            if (enemyPrefab == null) return;

            var pos = GetSpawnPosition();
            pos.y = 0.5f;
            var go = Instantiate(enemyPrefab, pos, Quaternion.identity);

            go.transform.localScale = definition.scale;

            var mf = go.GetComponentInChildren<MeshFilter>();
            if (mf != null && definition.mesh != null)
                mf.sharedMesh = definition.mesh;

            var mr = go.GetComponentInChildren<MeshRenderer>();
            if (mr != null && definition.material != null)
            {
                mr.material = definition.material;
                mr.material.SetColor("_BaseColor", definition.color);
            }

            var controller = go.GetComponent<EnemyController>();
            controller?.Setup(definition);

            var health = go.GetComponent<EnemyHealth>();
            health?.SetInitialHP(definition.maxHP);

            var ai = go.GetComponent<EnemyAI>();
            ai?.Setup(0);

            if (tracked)
            {
                bool despawned = false;
                void NotifyDespawn()
                {
                    if (despawned) return;
                    despawned = true;
                    OnEnemyDespawned?.Invoke();
                }
                if (controller != null) controller.OnReachedObjective += NotifyDespawn;
                if (health != null) health.OnDeath += NotifyDespawn;
            }
        }
    }
}
