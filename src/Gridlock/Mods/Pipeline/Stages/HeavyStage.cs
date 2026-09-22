namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class HeavyStage : IModStage
    {
        public float DamageMultiplier { get; set; } = 1.5f;
        public float SpeedMultiplier { get; set; } = 0.7f;
        public float SizeMultiplier { get; set; } = 1.3f;

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            ctx.Damage *= DamageMultiplier;
            ctx.Speed *= SpeedMultiplier;
            ctx.Size *= SizeMultiplier;
        }

        public IModStage Clone() => new HeavyStage
        {
            DamageMultiplier = DamageMultiplier,
            SpeedMultiplier = SpeedMultiplier,
            SizeMultiplier = SizeMultiplier,
        };
    }
}
