using System;
using AIWE.Combat;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerHealth : NetworkBehaviour, IDamageable
    {
        [SerializeField] private float _maxHP = 100f;

        private readonly NetworkVariable<float> _currentHP = new();

        public float CurrentHP => _currentHP.Value;
        public float MaxHP => _maxHP;
        public float HPNormalized => _maxHP > 0f ? _currentHP.Value / _maxHP : 0f;
        public bool IsAlive => _currentHP.Value > 0f;

        public event Action<float, float> OnHPChanged;
        public event Action OnDeath;

        public override void OnNetworkSpawn()
        {
            if (IsServer) _currentHP.Value = _maxHP;
            _currentHP.OnValueChanged += HandleHPChanged;
        }

        public override void OnNetworkDespawn()
        {
            _currentHP.OnValueChanged -= HandleHPChanged;
        }

        private void HandleHPChanged(float previous, float current)
        {
            OnHPChanged?.Invoke(current, _maxHP);
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsServer || !IsAlive) return;
            _currentHP.Value = Mathf.Max(0f, _currentHP.Value - damage.Amount);
            if (_currentHP.Value <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (!IsServer || !IsAlive) return;
            _currentHP.Value = Mathf.Min(_maxHP, _currentHP.Value + amount);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            Debug.Log($"[Player] {gameObject.name} died");
        }
    }
}
