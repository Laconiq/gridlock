namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class ImpactFeedbackStage : IModStage
    {
        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx) { }

        public IModStage Clone() => new ImpactFeedbackStage();
    }
}
