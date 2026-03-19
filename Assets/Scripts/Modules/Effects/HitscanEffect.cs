using System;
using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
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

                var damageable = target.Transform.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(new DamageInfo(damage, 0, DamageType.Hitscan));
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new HitscanEffect { damage = damage };
        }
    }
}
