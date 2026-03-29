using System;
using System.Collections;
using Gridlock.Combat;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float deathAnimDuration = 2f;

        private float _currentHP = 100f;
        private StatusEffectManager _statusEffects;

        public float CurrentHP => _currentHP;
        public float MaxHP { get; private set; } = 100f;
        public bool IsAlive => _currentHP > 0f;

        public event Action OnDeath;
        public event Action<float> CurrentHPChanged;

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
            {
                CurrentHPChanged?.Invoke(previous - _currentHP);

                var juice = Visual.GameJuice.Instance;
                if (juice != null)
                    juice.OnEnemyHit(transform.position);
            }

            if (_currentHP <= 0f)
                Die();
        }

        private void Die()
        {
            var stats = Core.GameStats.Instance;
            if (stats != null) stats.AddKill();

            var juice = Visual.GameJuice.Instance;
            if (juice != null)
                juice.OnEnemyKilled(transform.position);

            OnDeath?.Invoke();
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
    }
}
