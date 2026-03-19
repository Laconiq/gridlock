using System;
using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class DotEffect : EffectInstance
    {
        [SerializeField] private float damagePerTick = 5f;
        [SerializeField] private float duration = 4f;
        [SerializeField] private float tickInterval = 0.5f;

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
                        Type = StatusEffectType.DamageOverTime,
                        Value = damagePerTick,
                        Duration = duration,
                        TickInterval = tickInterval
                    });
                }
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new DotEffect
            {
                damagePerTick = damagePerTick,
                duration = duration,
                tickInterval = tickInterval
            };
        }
    }
}
