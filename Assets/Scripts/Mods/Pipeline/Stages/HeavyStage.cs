using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class HeavyStage : IModStage
    {
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private float speedMultiplier = 0.6f;
        [SerializeField] private float sizeMultiplier = 1.5f;

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            ctx.Damage *= damageMultiplier;
            ctx.Speed *= speedMultiplier;
            ctx.Size *= sizeMultiplier;
        }

        public IModStage Clone() => new HeavyStage
        {
            damageMultiplier = damageMultiplier,
            speedMultiplier = speedMultiplier,
            sizeMultiplier = sizeMultiplier,
        };
    }
}
