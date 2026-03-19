using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    public class HitscanEffect : EffectInstance
    {
        private readonly float _damage;

        public HitscanEffect(float damage)
        {
            _damage = damage;
        }

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;

                var damageable = target.Transform.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(new DamageInfo(_damage, 0, DamageType.Hitscan));
                }
            }
        }
    }
}
