using System.Numerics;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class OnOverkillEventStage : IModStage
    {
        public ModPipeline? SubPipeline;
        public float DamageScale = 0.6f;

        public StagePhase Phase => StagePhase.PostHit;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.OverkillAmount <= 0f || SubPipeline == null) return;
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

        public IModStage Clone() => new OnOverkillEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale
        };
    }
}
