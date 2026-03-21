using System;
using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class DamageBoostEffect : EffectInstance
    {
        [SerializeField] private float damageMultiplier = 1.5f;
        [SerializeField] private float duration = 5f;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var statusManager = target.Transform.GetComponent<StatusEffectManager>();
                if (statusManager != null)
                {
                    statusManager.ApplyEffect(new StatusEffectData
                    {
                        Type = StatusEffectType.DamageBoost,
                        Value = damageMultiplier,
                        Duration = duration
                    });
                }
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new DamageBoostEffect { damageMultiplier = damageMultiplier, duration = duration, cooldown = cooldown };
        }
    }
}
