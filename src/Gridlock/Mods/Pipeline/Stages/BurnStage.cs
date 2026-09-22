using Gridlock.Combat;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class BurnStage : IModStage
    {
        public float BurnDamage { get; set; } = 3f;
        public float BurnDuration { get; set; } = 3f;
        public float BurnTickInterval { get; set; } = 0.5f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var sem = ctx.HitTarget?.StatusEffects;
            if (sem == null) return;

            sem.ApplyEffect(new StatusEffectData
            {
                Type = StatusEffectType.DamageOverTime,
                Value = BurnDamage,
                Duration = BurnDuration,
                TickInterval = BurnTickInterval
            });
        }

        public IModStage Clone() => new BurnStage
        {
            BurnDamage = BurnDamage,
            BurnDuration = BurnDuration,
            BurnTickInterval = BurnTickInterval
        };
    }
}
