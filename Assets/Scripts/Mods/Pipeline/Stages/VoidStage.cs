using System;
using Gridlock.Combat;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class VoidStage : IModStage
    {
        [SerializeField] private float hpPercent = 0.05f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var health = ctx.HitObject.GetComponentInParent<EnemyHealth>();
            if (health == null) return;

            float voidDamage = health.CurrentHP * hpPercent;
            var damageable = ctx.HitObject.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(new DamageInfo(voidDamage, DamageType.Direct));

            if (ctx.Synergies.Contains(SynergyEffect.Siphon))
                ObjectiveController.Instance?.Heal(voidDamage);
        }

        public IModStage Clone() => new VoidStage { hpPercent = hpPercent };
    }
}
