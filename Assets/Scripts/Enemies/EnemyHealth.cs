using System;
using AIWE.Combat;
using AIWE.Interfaces;
using AIWE.Loot;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyHealth : NetworkBehaviour, IDamageable
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField] private GameObject pickupPrefab;

        private readonly NetworkVariable<float> _currentHP = new(100f);

        public float CurrentHP => _currentHP.Value;
        public float MaxHP { get; private set; } = 100f;
        public bool IsAlive => _currentHP.Value > 0f;

        public event Action OnDeath;

        public void SetMaxHP(float maxHP)
        {
            MaxHP = maxHP;
            if (IsServer)
                _currentHP.Value = maxHP;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsServer || !IsAlive) return;

            _currentHP.Value = Mathf.Max(0f, _currentHP.Value - damage.Amount);

            if (_currentHP.Value <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            OnDeath?.Invoke();
            SpawnDrop();
            Debug.Log($"[Enemy] {gameObject.name} died");
            NetworkObject.Despawn();
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
