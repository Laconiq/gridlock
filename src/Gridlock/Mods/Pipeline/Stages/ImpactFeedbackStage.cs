using System;
using Gridlock.Audio;
using Gridlock.Core;
using Gridlock.Visual;
using Raylib_cs;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class ImpactFeedbackStage : IModStage
    {
        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var color = ModTagsUtil.GetColor(ctx.Tags);
            float intensity = MathF.Min(ctx.Damage / 30f, 1f);

            ImpactFlash.Instance?.Spawn(ctx.Position, color, 0.3f + 0.2f * intensity, 0.12f);

            int particleCount = (int)(4f + 8f * intensity);
            float particleSpeed = 2f + 4f * intensity;
            ParticleEmitter.Instance?.BurstSphere(ctx.Position, particleCount, particleSpeed, 5f, 0.15f, color);

            ServiceLocator.Get<SoundManager>()?.Play(SoundType.EnemyHit, worldPos: ctx.Position);

            GameJuice.Instance?.OnEnemyHit(ctx.Position);

            VoxelDeathEffect.ShedOnHit(ctx.Position, color, ctx.Damage, ctx.HitTarget?.MaxHP ?? 100f);

            DamageTextSystem.Instance?.Spawn(ctx.Position, ctx.Damage, color);
        }

        public IModStage Clone() => new ImpactFeedbackStage();
    }
}
