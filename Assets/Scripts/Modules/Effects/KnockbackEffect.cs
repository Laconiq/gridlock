using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class KnockbackEffect : EffectInstance
    {
        [SerializeField] private float force = 5f;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var direction = (target.Position - origin).normalized;
                target.Transform.position += direction * force;
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new KnockbackEffect { force = force, cooldown = cooldown };
        }
    }
}
