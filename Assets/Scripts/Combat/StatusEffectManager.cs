using System.Collections.Generic;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Combat
{
    public class StatusEffectManager : MonoBehaviour
    {
        private readonly List<ActiveStatusEffect> _activeEffects = new();
        private IDamageable _damageable;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;
        public float VulnerabilityMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            _damageable = GetComponent<IDamageable>();
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

        private void Update()
        {
            bool changed = false;

            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.RemainingDuration -= Time.deltaTime;

                if (effect.Data.Type == StatusEffectType.DamageOverTime)
                {
                    effect.TickTimer += Time.deltaTime;
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

        private class ActiveStatusEffect
        {
            public StatusEffectData Data;
            public float RemainingDuration;
            public float TickTimer;
        }
    }

    public struct StatusEffectData
    {
        public StatusEffectType Type;
        public float Value;
        public float Duration;
        public float TickInterval;
    }

    public enum StatusEffectType
    {
        Slow,
        DamageOverTime,
        Weaken,
        Vulnerability,
        SpeedBoost,
        DamageBoost
    }
}
