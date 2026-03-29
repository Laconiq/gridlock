using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class OnPulseEventStage : IModStage
    {
        public ModPipeline SubPipeline;
        public float DamageScale = 0.6f;
        public float Interval = 0.3f;

        public StagePhase Phase => StagePhase.OnUpdate;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null) return;
            ctx.PulseTimer += Time.deltaTime;
            if (ctx.PulseTimer < Interval) return;
            ctx.PulseTimer -= Interval;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.x, 0.5f, ctx.Position.z),
                Direction = ctx.Direction,
                Pipeline = SubPipeline.Clone(),
                DamageScale = DamageScale,
                Target = ctx.Target
            });
        }

        public IModStage Clone() => new OnPulseEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale,
            Interval = Interval
        };
    }
}
