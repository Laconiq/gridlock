using Gridlock.Combat;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class VoidStage : IModStage
    {
        public float HpPercent { get; set; } = 0.05f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.HitTarget == null) return;

            float voidDamage = ctx.HitTarget.CurrentHP * HpPercent;
            ctx.HitTarget.Damageable?.TakeDamage(new DamageInfo(voidDamage, DamageType.Direct));

            if (ctx.Synergies.Contains(SynergyEffect.Siphon))
                ctx.ObjectiveHealer?.Heal(voidDamage);
        }

        public IModStage Clone() => new VoidStage { HpPercent = HpPercent };
    }
}
