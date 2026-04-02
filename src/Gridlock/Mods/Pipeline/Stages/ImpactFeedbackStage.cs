using System;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class ImpactFeedbackStage : IModStage
    {
        public StagePhase Phase => StagePhase.OnHit;

        public static Action<ModContext>? OnImpact;

        public void Execute(ref ModContext ctx)
        {
            OnImpact?.Invoke(ctx);
        }

        public IModStage Clone() => new ImpactFeedbackStage();
    }
}
