using System;
using System.Collections;
using AIWE.AI;
using AIWE.Combat;
using AIWE.Interfaces;
using AIWE.Loot;
using AIWE.Network;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyHealth : NetworkBehaviour, IDamageable
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField] private GameObject pickupPrefab;

        private readonly NetworkVariable<float> _currentHP = new(100f);
        private StatusEffectManager _statusEffects;
        private ulong _lastDamageSourceId;

        public float CurrentHP => _currentHP.Value;
        public float MaxHP { get; private set; } = 100f;
        public bool IsAlive => _currentHP.Value > 0f;

        public event Action OnDeath;
        public event Action<float> _currentHPChanged;

        private void Awake()
        {
            _statusEffects = GetComponent<StatusEffectManager>();
        }

        public override void OnNetworkSpawn()
        {
            _currentHP.OnValueChanged += HandleHPChanged;
        }

        public override void OnNetworkDespawn()
        {
            _currentHP.OnValueChanged -= HandleHPChanged;
        }

        private void HandleHPChanged(float previous, float current)
        {
            if (current < previous)
                _currentHPChanged?.Invoke(previous - current);
        }

        public void SetInitialHP(float maxHP)
        {
            MaxHP = maxHP;
            _currentHP.Value = maxHP;
        }

        public void SetMaxHP(float maxHP)
        {
            MaxHP = maxHP;
            if (IsServer)
                _currentHP.Value = maxHP;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsServer || !IsAlive) return;

            _lastDamageSourceId = damage.SourceId;

            float amount = damage.Amount;
            if (_statusEffects != null)
                amount *= _statusEffects.VulnerabilityMultiplier;

            _currentHP.Value = Mathf.Max(0f, _currentHP.Value - amount);

            ThreatSource.ReportDamageFromSource(damage.SourceId, amount);

            if (_currentHP.Value <= 0f)
            {
                Die();
            }
        }

        [SerializeField] private float deathAnimDuration = 2f;

        private void Die()
        {
            AttributeKill(_lastDamageSourceId);
            OnDeath?.Invoke();
            SpawnDrop();
            StartCoroutine(DespawnAfterDeathAnim());
        }

        private IEnumerator DespawnAfterDeathAnim()
        {
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
                agent.enabled = false;
            }

            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            yield return new WaitForSeconds(deathAnimDuration);

            if (NetworkObject != null && NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }

        private void AttributeKill(ulong sourceId)
        {
            if (sourceId == 0) return;
            if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                    .TryGetValue(sourceId, out var netObj) == true)
            {
                var playerData = netObj.GetComponent<PlayerData>();
                playerData?.AddKill();
            }
        }

        private void SpawnDrop()
        {
            if (!IsServer || lootTable == null || pickupPrefab == null) return;

            var drop = lootTable.Roll();
            if (drop == null) return;

            var go = Instantiate(pickupPrefab, transform.position, Quaternion.identity);

            var pickup = go.GetComponent<ModulePickup>();
            pickup?.Initialize(drop.moduleId, transform.position);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }
    }
}
