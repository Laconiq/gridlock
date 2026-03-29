using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class OnDelayEventStage : IModStage
    {
        public ModPipeline SubPipeline;
        public float DamageScale = 0.6f;
        public float Delay = 0.5f;

        public StagePhase Phase => StagePhase.OnUpdate;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.DelayFired || SubPipeline == null) return;
            if (ctx.Lifetime < Delay) return;
            ctx.DelayFired = true;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.x, 0.5f, ctx.Position.z),
                Direction = ctx.Direction,
                Pipeline = SubPipeline.Clone(),
                DamageScale = DamageScale,
                Target = ctx.Target
            });
            ctx.Consumed = true;
        }

        public IModStage Clone() => new OnDelayEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale,
            Delay = Delay
        };
    }
}
