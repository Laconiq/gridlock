using System;
using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class SpeedBoostEffect : EffectInstance
    {
        [SerializeField] private float speedMultiplier = 1.5f;
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
                        Type = StatusEffectType.SpeedBoost,
                        Value = speedMultiplier,
                        Duration = duration
                    });
                }
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new SpeedBoostEffect { speedMultiplier = speedMultiplier, duration = duration, cooldown = cooldown };
        }
    }
}
