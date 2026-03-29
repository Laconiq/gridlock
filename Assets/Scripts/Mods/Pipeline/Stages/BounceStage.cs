using System;
using UnityEngine;
using Gridlock.Enemies;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class BounceStage : IModStage
    {
        public StagePhase Phase => StagePhase.PostHit;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.BounceRemaining <= 0) return;

            float bestDist = float.MaxValue;
            EnemyEntry? best = null;

            foreach (var entry in EnemyRegistry.All)
            {
                if (!entry.Controller.IsAlive) continue;
                if (ctx.HitInstances != null && ctx.HitInstances.Contains(entry.Controller.GetInstanceID())) continue;

                var diff = entry.Controller.Position - ctx.Position;
                diff.y = 0f;
                float dist = diff.sqrMagnitude;
                if (dist < bestDist) { bestDist = dist; best = entry; }
            }

            if (!best.HasValue) return;

            ctx.BounceRemaining--;
            ctx.Target = best.Value.Controller;
            var dir = best.Value.Controller.Position - ctx.Position;
            dir.y = 0f;
            ctx.Direction = dir.normalized;
            ctx.Consumed = false;
        }

        public IModStage Clone() => new BounceStage();
    }
}
