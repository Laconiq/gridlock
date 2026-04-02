using Gridlock.Combat;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class FrostStage : IModStage
    {
        public float SlowValue { get; set; } = 0.65f;
        public float Duration { get; set; } = 2f;
        public float BlizzardStunDuration { get; set; } = 0.5f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var sem = ctx.HitTarget?.StatusEffects;
            if (sem == null) return;

            sem.ApplyEffect(new StatusEffectData
            {
                Type = StatusEffectType.Slow,
                Value = SlowValue,
                Duration = Duration
            });

            if (ctx.Synergies.Contains(SynergyEffect.Blizzard))
            {
                sem.ApplyEffect(new StatusEffectData
                {
                    Type = StatusEffectType.Slow,
                    Value = 0f,
                    Duration = BlizzardStunDuration
                });
            }
        }

        public IModStage Clone() => new FrostStage
        {
            SlowValue = SlowValue,
            Duration = Duration,
            BlizzardStunDuration = BlizzardStunDuration
        };
    }
}
