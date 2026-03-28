using System;
using System.Collections;
using AIWE.AI;
using AIWE.Combat;
using AIWE.Interfaces;
using AIWE.Loot;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private LootTable lootTable;
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private float deathAnimDuration = 2f;

        private float _currentHP = 100f;
        private StatusEffectManager _statusEffects;

        public float CurrentHP => _currentHP;
        public float MaxHP { get; private set; } = 100f;
        public bool IsAlive => _currentHP > 0f;

        public event Action OnDeath;
        public event Action<float> _currentHPChanged;

        private void Awake()
        {
            _statusEffects = GetComponent<StatusEffectManager>();
        }

        public void SetInitialHP(float maxHP)
        {
            MaxHP = maxHP;
            _currentHP = maxHP;
        }

        public void SetMaxHP(float maxHP)
        {
            MaxHP = maxHP;
            _currentHP = maxHP;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;

            float amount = damage.Amount;
            if (_statusEffects != null)
                amount *= _statusEffects.VulnerabilityMultiplier;

            float previous = _currentHP;
            _currentHP = Mathf.Max(0f, _currentHP - amount);

            if (_currentHP < previous)
                _currentHPChanged?.Invoke(previous - _currentHP);

            ThreatSource.ReportDamageFromSource(damage.SourceId, amount);

            if (_currentHP <= 0f)
                Die();
        }

        private void Die()
        {
            var stats = Core.GameStats.Instance;
            if (stats != null) stats.AddKill();

            OnDeath?.Invoke();
            SpawnDrop();
            StartCoroutine(DespawnAfterDeathAnim());
        }

        private IEnumerator DespawnAfterDeathAnim()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            yield return new WaitForSeconds(deathAnimDuration);

            Destroy(gameObject);
        }

        private void SpawnDrop()
        {
            if (lootTable == null || pickupPrefab == null) return;

            var drop = lootTable.Roll();
            if (drop == null) return;

            var go = Instantiate(pickupPrefab, transform.position, Quaternion.identity);

            var pickup = go.GetComponent<ModulePickup>();
            pickup?.Initialize(drop.moduleId, transform.position);
        }
    }
}
