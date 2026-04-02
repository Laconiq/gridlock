using System.Numerics;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class OnDelayEventStage : IModStage
    {
        public ModPipeline? SubPipeline;
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
                Origin = new Vector3(ctx.Position.X, 0.5f, ctx.Position.Z),
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
