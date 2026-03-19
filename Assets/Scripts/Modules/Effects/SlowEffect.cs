using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    public class SlowEffect : EffectInstance
    {
        private readonly float _slowFactor;
        private readonly float _duration;

        public SlowEffect(float slowFactor, float duration)
        {
            _slowFactor = slowFactor;
            _duration = duration;
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
                        Type = StatusEffectType.Slow,
                        Value = _slowFactor,
                        Duration = _duration
                    });
                }
            }
        }
    }
}
