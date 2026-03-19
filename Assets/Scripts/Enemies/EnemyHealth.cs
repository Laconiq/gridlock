using System;
using AIWE.Combat;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyHealth : NetworkBehaviour, IDamageable
    {
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
            Debug.Log($"[Enemy] {gameObject.name} died");
            NetworkObject.Despawn();
        }
    }
}
