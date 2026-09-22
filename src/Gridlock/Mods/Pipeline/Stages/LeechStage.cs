namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class LeechStage : IModStage
    {
        public float LeechPercent { get; set; } = 0.12f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            float percent = LeechPercent;
            if (ctx.Synergies != null && ctx.Synergies.Contains(SynergyEffect.Vampire))
                percent = 0.4f;
            ctx.ObjectiveHealer?.Heal(ctx.Damage * percent);
        }

        public IModStage Clone() => new LeechStage { LeechPercent = LeechPercent };
    }
}
