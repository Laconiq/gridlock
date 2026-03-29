using System;
using Gridlock.Combat;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class FrostStage : IModStage
    {
        [SerializeField] private float slowValue = 0.5f;
        [SerializeField] private float duration = 2f;
        [SerializeField] private float blizzardStunDuration = 0.5f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var sem = ctx.HitObject.GetComponentInParent<StatusEffectManager>();
            if (sem == null) return;

            sem.ApplyEffect(new StatusEffectData
            {
                Type = StatusEffectType.Slow,
                Value = slowValue,
                Duration = duration
            });

            if (ctx.Synergies.Contains(SynergyEffect.Blizzard))
            {
                sem.ApplyEffect(new StatusEffectData
                {
                    Type = StatusEffectType.Slow,
                    Value = 0f,
                    Duration = blizzardStunDuration
                });
            }
        }

        public IModStage Clone() => new FrostStage
        {
            slowValue = slowValue,
            duration = duration,
            blizzardStunDuration = blizzardStunDuration
        };
    }
}
