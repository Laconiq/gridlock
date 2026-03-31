using System;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class OnHitEventStage : IModStage
    {
        public ModPipeline SubPipeline;
        public float DamageScale = 0.6f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null) return;
            var target = ctx.HitObject != null ? ctx.HitObject.GetComponent<ITargetable>() : ctx.Target;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.x, 0.5f, ctx.Position.z),
                Direction = SpawnRequest.RandomDirectionExcluding(ctx.Direction),
                Pipeline = SubPipeline.Clone(),
                DamageScale = DamageScale,
                Target = target
            });
        }

        public IModStage Clone() => new OnHitEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale
        };
    }
}
