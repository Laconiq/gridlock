using System;
using UnityEngine;
using Gridlock.Combat;
using Gridlock.Enemies;
using Gridlock.Interfaces;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class WideStage : IModStage
    {
        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            float radius = ctx.WideRadius;
            if (ctx.Synergies != null && ctx.Synergies.Contains(SynergyEffect.Meteor))
                radius *= 2f;

            float radiusSqr = radius * radius;
            var dmg = new DamageInfo(ctx.Damage, DamageType.Projectile);

            foreach (var entry in EnemyRegistry.All)
            {
                if (!entry.Controller.IsAlive) continue;
                if (ctx.HitInstances != null && ctx.HitInstances.Contains(entry.Controller.GetInstanceID())) continue;

                var diff = entry.Controller.Position - ctx.Position;
                diff.y = 0f;
                if (diff.sqrMagnitude > radiusSqr) continue;

                entry.Health.TakeDamage(dmg);
            }
        }

        public IModStage Clone() => new WideStage();
    }
}
