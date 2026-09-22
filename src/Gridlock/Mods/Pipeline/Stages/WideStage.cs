using System.Collections.Generic;
using System.Numerics;
using Gridlock.Combat;
using Gridlock.Enemies;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class WideStage : IModStage
    {
        public StagePhase Phase => StagePhase.OnHit;

        [System.ThreadStatic] private static List<Enemy>? _buffer;

        public void Execute(ref ModContext ctx)
        {
            float radius = ctx.WideRadius;
            if (ctx.Synergies.Contains(SynergyEffect.Meteor))
                radius *= 2f;

            float radiusSqr = radius * radius;
            var dmg = new DamageInfo(ctx.Damage, DamageType.Projectile);

            var entries = _buffer ??= new List<Enemy>(32);
            entries.Clear();
            EnemyRegistry.Spatial.QuerySegment(ctx.Position, ctx.Position, radius, entries);
            for (int i = 0; i < entries.Count; i++)
            {
                var enemy = entries[i];
                if (!enemy.IsAlive) continue;

                if (ctx.HitInstances.Contains(enemy.EntityId)) continue;

                var diff = enemy.Position - ctx.Position;
                diff = new Vector3(diff.X, 0f, diff.Z);
                if (diff.LengthSquared() > radiusSqr) continue;

                ctx.HitInstances.Add(enemy.EntityId);
                enemy.Damageable?.TakeDamage(dmg);
            }
        }

        public IModStage Clone() => new WideStage();
    }
}
