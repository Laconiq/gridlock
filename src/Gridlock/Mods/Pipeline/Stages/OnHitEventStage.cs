using System.Numerics;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class OnHitEventStage : IModStage
    {
        public ModPipeline? SubPipeline;
        public float DamageScale = 0.6f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null) return;
            var target = ctx.HitTarget ?? ctx.Target;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.X, 0.5f, ctx.Position.Z),
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
