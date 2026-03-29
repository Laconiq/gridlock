using System;
using Gridlock.Combat;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class BurnStage : IModStage
    {
        [SerializeField] private float burnDamage = 5f;
        [SerializeField] private float burnDuration = 3f;
        [SerializeField] private float burnTickInterval = 0.5f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var sem = ctx.HitObject.GetComponentInParent<StatusEffectManager>();
            if (sem == null) return;

            sem.ApplyEffect(new StatusEffectData
            {
                Type = StatusEffectType.DamageOverTime,
                Value = burnDamage,
                Duration = burnDuration,
                TickInterval = burnTickInterval
            });
        }

        public IModStage Clone() => new BurnStage
        {
            burnDamage = burnDamage,
            burnDuration = burnDuration,
            burnTickInterval = burnTickInterval
        };
    }
}
