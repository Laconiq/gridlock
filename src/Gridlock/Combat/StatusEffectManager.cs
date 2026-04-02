using System.Collections.Generic;

namespace Gridlock.Combat
{
    public sealed class StatusEffectManager
    {
        private readonly List<ActiveStatusEffect> _activeEffects = new();
        private IDamageable? _damageable;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;
        public float VulnerabilityMultiplier { get; private set; } = 1f;

        public StatusEffectManager() : this(null) { }

        public StatusEffectManager(IDamageable? damageable)
        {
            _damageable = damageable;
        }

        public void SetDamageable(IDamageable? damageable)
        {
            _damageable = damageable;
        }

        public bool HasEffectOfType(StatusEffectType type)
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].Data.Type == type)
                    return true;
            }
            return false;
        }

        public void ApplyEffect(StatusEffectData data)
        {
            _activeEffects.Add(new ActiveStatusEffect
            {
                Data = data,
                RemainingDuration = data.Duration,
                TickTimer = 0f
            });

            RecalculateModifiers();
        }

        public void Update(float dt)
        {
            bool changed = false;

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.RemainingDuration -= dt;

                if (effect.Data.Type == StatusEffectType.DamageOverTime && effect.Data.TickInterval > 0f)
                {
                    effect.TickTimer += dt;
                    if (effect.TickTimer >= effect.Data.TickInterval)
                    {
                        effect.TickTimer = 0f;
                        _damageable?.TakeDamage(new DamageInfo(
                            effect.Data.Value, DamageType.DamageOverTime));
                    }
                }

                if (effect.RemainingDuration <= 0f)
                {
                    _activeEffects.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed) RecalculateModifiers();
        }

        private void RecalculateModifiers()
        {
            SpeedMultiplier = 1f;
            DamageMultiplier = 1f;
            VulnerabilityMultiplier = 1f;

            foreach (var effect in _activeEffects)
            {
                switch (effect.Data.Type)
                {
                    case StatusEffectType.Slow:
                    case StatusEffectType.SpeedBoost:
                        SpeedMultiplier *= effect.Data.Value;
                        break;
                    case StatusEffectType.Weaken:
                    case StatusEffectType.DamageBoost:
                        DamageMultiplier *= effect.Data.Value;
                        break;
                    case StatusEffectType.Vulnerability:
                        VulnerabilityMultiplier *= effect.Data.Value;
                        break;
                }
            }
        }

        private sealed class ActiveStatusEffect
        {
            public StatusEffectData Data;
            public float RemainingDuration;
            public float TickTimer;
        }
    }
}
