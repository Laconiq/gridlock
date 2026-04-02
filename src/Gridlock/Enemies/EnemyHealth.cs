using System;
using Gridlock.Combat;

namespace Gridlock.Enemies
{
    public sealed class EnemyHealth : IDamageable
    {
        private float _currentHP;
        private readonly float _deathAnimDuration;
        private float _deathTimer;
        private bool _dying;
        private StatusEffectManager? _statusEffects;

        public float CurrentHP => _currentHP;
        public float MaxHP { get; private set; }
        public bool IsAlive => _currentHP > 0f;
        public bool PendingRemoval => _dying && _deathTimer <= 0f;
        public float LastHitTime { get; private set; } = -1f;

        public event Action? OnDeath;
        public event Action<float>? CurrentHPChanged;

        public EnemyHealth(float maxHP, float deathAnimDuration = 2f)
        {
            MaxHP = maxHP;
            _currentHP = maxHP;
            _deathAnimDuration = deathAnimDuration;
        }

        public void SetStatusEffects(StatusEffectManager statusEffects)
        {
            _statusEffects = statusEffects;
        }

        public void SetMaxHP(float maxHP)
        {
            MaxHP = maxHP;
            _currentHP = maxHP;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive) return;

            float vuln = _statusEffects?.VulnerabilityMultiplier ?? 1f;
            float amount = damage.Amount * vuln;
            float previous = _currentHP;
            _currentHP = MathF.Max(0f, _currentHP - amount);

            if (_currentHP < previous)
            {
                LastHitTime = (float)Raylib_cs.Raylib.GetTime();
                CurrentHPChanged?.Invoke(previous - _currentHP);
            }

            if (_currentHP <= 0f)
                Die();
        }

        private void Die()
        {
            _dying = true;
            _deathTimer = _deathAnimDuration;
            Core.GameStats.Instance?.AddKill();
            OnDeath?.Invoke();
        }

        public void ForceKill()
        {
            if (_dying) return;
            _currentHP = 0;
            Die();
        }

        public void Update(float dt)
        {
            if (_dying)
                _deathTimer -= dt;
        }

        public void Reset(float maxHP)
        {
            MaxHP = maxHP;
            _currentHP = maxHP;
            _dying = false;
            _deathTimer = 0f;
            LastHitTime = -1f;
            OnDeath = null;
            CurrentHPChanged = null;
        }
    }
}
