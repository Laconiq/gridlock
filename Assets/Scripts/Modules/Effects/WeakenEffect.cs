using System;
using System.Collections.Generic;
using Gridlock.Combat;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Effects
{
    [Serializable]
    public class WeakenEffect : EffectInstance
    {
        [SerializeField, Range(0f, 1f)] private float damageReduction = 0.5f;
        [SerializeField] private float duration = 3f;

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
                        Type = StatusEffectType.Weaken,
                        Value = damageReduction,
                        Duration = duration
                    });
                }
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new WeakenEffect { damageReduction = damageReduction, duration = duration, cooldown = cooldown };
        }
    }
}
