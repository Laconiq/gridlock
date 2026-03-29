using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class PierceStage : IModStage
    {
        public StagePhase Phase => StagePhase.PostHit;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.PierceRemaining <= 0) return;

            ctx.PierceRemaining--;
            ctx.Consumed = false;
        }

        public IModStage Clone() => new PierceStage();
    }
}
