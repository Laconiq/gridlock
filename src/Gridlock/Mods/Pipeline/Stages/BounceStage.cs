using System.Numerics;
using Gridlock.Combat;
using Gridlock.Enemies;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class BounceStage : IModStage
    {
        public StagePhase Phase => StagePhase.PostHit;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.BounceRemaining <= 0) return;

            float bestDist = float.MaxValue;
            ITargetable? best = null;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var enemy = entries[i];
                if (!enemy.IsAlive) continue;
                if (ctx.HitInstances != null && ctx.HitInstances.Contains(enemy.EntityId)) continue;

                var diff = enemy.Position - ctx.Position;
                diff = new Vector3(diff.X, 0f, diff.Z);
                float dist = diff.LengthSquared();
                if (dist < bestDist) { bestDist = dist; best = enemy; }
            }

            if (best == null) return;

            ctx.BounceRemaining--;
            ctx.Target = best;
            var dir = best.Position - ctx.Position;
            dir = new Vector3(dir.X, 0f, dir.Z);
            ctx.Direction = Vector3.Normalize(dir);
            ctx.Consumed = false;
        }

        public IModStage Clone() => new BounceStage();
    }
}
