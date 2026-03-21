using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Player;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class HealEffect : EffectInstance
    {
        [SerializeField] private float healAmount = 25f;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var health = target.Transform.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(healAmount);
                }
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new HealEffect { healAmount = healAmount, cooldown = cooldown };
        }
    }
}
