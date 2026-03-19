using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    public class DotEffect : EffectInstance
    {
        private readonly float _damagePerTick;
        private readonly float _duration;
        private readonly float _tickInterval;

        public DotEffect(float damagePerTick, float duration, float tickInterval = 0.5f)
        {
            _damagePerTick = damagePerTick;
            _duration = duration;
            _tickInterval = tickInterval;
        }

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;

                var statusManager = target.Transform.GetComponent<StatusEffectManager>();
                if (statusManager != null)
                {
                    statusManager.ApplyEffect(new StatusEffectData
                    {
                        Type = StatusEffectType.DamageOverTime,
                        Value = _damagePerTick,
                        Duration = _duration,
                        TickInterval = _tickInterval
                    });
                }
            }
        }
    }
}
