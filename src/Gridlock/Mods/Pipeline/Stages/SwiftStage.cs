namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class SwiftStage : IModStage
    {
        public float DamageMultiplier { get; set; } = 0.75f;
        public float SpeedMultiplier { get; set; } = 1.5f;

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            ctx.Damage *= DamageMultiplier;
            ctx.Speed *= SpeedMultiplier;
        }

        public IModStage Clone() => new SwiftStage
        {
            DamageMultiplier = DamageMultiplier,
            SpeedMultiplier = SpeedMultiplier,
        };
    }
}
