using System;
using System.Numerics;
using Gridlock.Combat;
using Gridlock.Enemies;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class HomingStage : IModStage
    {
        public float TurnSpeed { get; set; } = 8f;

        public StagePhase Phase => StagePhase.OnUpdate;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.Target == null || !ctx.Target.IsAlive)
            {
                ctx.Target = FindNearest(ref ctx);
                if (ctx.Target == null) return;
            }

            var toTarget = ctx.Target.Position - ctx.Position;
            toTarget = new Vector3(toTarget.X, 0f, toTarget.Z);
            if (toTarget.LengthSquared() < 0.001f) return;

            var desired = Vector3.Normalize(toTarget);
            bool isMissile = ctx.Synergies != null && ctx.Synergies.Contains(SynergyEffect.Missile);
            ctx.Direction = isMissile
                ? desired
                : Vector3.Normalize(Vector3.Lerp(ctx.Direction, desired, TurnSpeed * ctx.DeltaTime));
        }

        private static ITargetable? FindNearest(ref ModContext ctx)
        {
            float bestDist = float.MaxValue;
            ITargetable? best = null;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var enemy = entries[i];
                if (!enemy.IsAlive) continue;
                if (ctx.HitInstances != null && ctx.HitInstances.Contains(enemy.EntityId))
                    continue;

                var diff = enemy.Position - ctx.Position;
                float dist = diff.X * diff.X + diff.Z * diff.Z;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }

            return best;
        }

        public IModStage Clone() => new HomingStage { TurnSpeed = TurnSpeed };
    }
}
