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
            var colors = Visual.ProjectileVisual.GetElementColors(ctx.Tags);
            var color = colors[0];
            float intensity = Mathf.Clamp01(ctx.Damage / 30f);

            ImpactFlash.Spawn(ctx.Position, color);
            Visual.ParticleVFX.ImpactBurst(ctx.Position, color, intensity);
            SoundManager.Instance?.Play(SoundType.ProjectileImpact, ctx.Position);
            GameJuice.Instance?.OnEnemyHit(ctx.Position);
        }

        public IModStage Clone() => new ImpactFeedbackStage();
    }
}
