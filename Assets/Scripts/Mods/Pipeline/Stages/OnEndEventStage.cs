using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class OnEndEventStage : IModStage
    {
        public ModPipeline SubPipeline;
        public float DamageScale = 0.6f;

        public StagePhase Phase => StagePhase.OnExpire;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null) return;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.x, 0.5f, ctx.Position.z),
                Direction = ctx.Direction,
                Pipeline = SubPipeline.Clone(),
                DamageScale = DamageScale,
                Target = ctx.Target
            });
        }

        public IModStage Clone() => new OnEndEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale
        };
    }
}
