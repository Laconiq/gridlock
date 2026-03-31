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

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Controller == null || !entry.Controller.IsAlive) continue;

                var id = entry.Controller.gameObject.GetEntityId();
                if (ctx.HitInstances != null && ctx.HitInstances.Contains(id)) continue;

                var diff = entry.Controller.Position - ctx.Position;
                diff.y = 0f;
                if (diff.sqrMagnitude > radiusSqr) continue;

                ctx.HitInstances?.Add(id);
                entry.Health.TakeDamage(dmg);
            }
        }

        public IModStage Clone() => new WideStage();
    }
}
