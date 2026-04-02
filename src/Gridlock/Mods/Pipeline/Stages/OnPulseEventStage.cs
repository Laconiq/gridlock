using System.Numerics;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class OnPulseEventStage : IModStage
    {
        public ModPipeline? SubPipeline;
        public float DamageScale = 0.6f;
        public float Interval = 0.3f;

        public StagePhase Phase => StagePhase.OnUpdate;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null) return;
            ctx.PulseTimer += ctx.DeltaTime;
            if (ctx.PulseTimer < Interval) return;
            ctx.PulseTimer -= Interval;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.X, 0.5f, ctx.Position.Z),
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
