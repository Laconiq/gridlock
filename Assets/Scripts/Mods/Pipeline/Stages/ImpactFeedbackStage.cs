using System;
using UnityEngine;
using Gridlock.Audio;
using Gridlock.Visual;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class ImpactFeedbackStage : IModStage
    {
        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            ImpactFlash.Spawn(ctx.Position, Color.cyan);
            SoundManager.Instance?.Play(SoundType.ProjectileImpact, ctx.Position);
            GameJuice.Instance?.OnEnemyHit(ctx.Position);
        }

        public IModStage Clone() => new ImpactFeedbackStage();
    }
}
