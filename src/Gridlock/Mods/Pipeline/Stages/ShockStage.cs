using System;
using System.Numerics;
using Gridlock.Combat;
using Gridlock.Enemies;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class ShockStage : IModStage
    {
        public int ChainCount { get; set; } = 1;
        public float ChainRadius { get; set; } = 3f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var origin = ctx.Position;
            int maxChains = ChainCount;
            if (ctx.Synergies != null && ctx.Synergies.Contains(SynergyEffect.Tesla))
                maxChains = 3;

            var entries = EnemyRegistry.All;

            for (int c = 0; c < maxChains && c < EnemyRegistry.Count; c++)
            {
                float bestDist = float.MaxValue;
                ITargetable? best = null;

                for (int i = 0; i < entries.Count; i++)
                {
                    var enemy = entries[i];
                    if (!enemy.IsAlive) continue;
                    if (ctx.HitInstances.Contains(enemy.EntityId)) continue;

                    var delta = enemy.Position - origin;
                    float dist = MathF.Sqrt(delta.X * delta.X + delta.Z * delta.Z);
                    if (dist > ChainRadius || dist >= bestDist) continue;

                    bestDist = dist;
                    best = enemy;
                }

                if (best == null) break;

                ctx.HitInstances.Add(best.EntityId);
                best.Damageable?.TakeDamage(new DamageInfo(ctx.Damage, DamageType.Direct));
                origin = best.Position;
            }
        }

        public IModStage Clone() => new ShockStage
        {
            ChainCount = ChainCount,
            ChainRadius = ChainRadius
        };
    }
}
