using System;
using System.Collections.Generic;
using Gridlock.Combat;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Effects
{
    [Serializable]
    public class HitscanEffect : EffectInstance
    {
        [SerializeField] private float damage = 20f;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var damageable = target.Transform.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(new DamageInfo(damage, DamageType.Hitscan));
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new HitscanEffect { damage = damage, cooldown = cooldown };
        }
    }
}
