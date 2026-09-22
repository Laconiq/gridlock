using System.Numerics;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class OnEndEventStage : IModStage
    {
        public ModPipeline? SubPipeline;
        public float DamageScale = 0.6f;

        public StagePhase Phase => StagePhase.OnExpire;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null) return;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.X, 0.5f, ctx.Position.Z),
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
