using System;
using Gridlock.Core;
using Gridlock.Enemies;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class VoidStage : IModStage
    {
        [SerializeField] private float hpPercent = 0.08f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var health = ctx.HitObject.GetComponentInParent<EnemyHealth>();
            if (health == null) return;

            float voidDamage = health.CurrentHP * hpPercent;
            ctx.Damage = voidDamage;

            if (ctx.Synergies.Contains(SynergyEffect.Siphon))
                ObjectiveController.Instance?.Heal(voidDamage);
        }

        public IModStage Clone() => new VoidStage { hpPercent = hpPercent };
    }
}
