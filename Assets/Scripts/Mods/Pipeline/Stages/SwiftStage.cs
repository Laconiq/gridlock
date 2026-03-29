using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class SwiftStage : IModStage
    {
        [SerializeField] private float damageMultiplier = 0.75f;
        [SerializeField] private float speedMultiplier = 1.5f;

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            ctx.Damage *= damageMultiplier;
            ctx.Speed *= speedMultiplier;
        }

        public IModStage Clone() => new SwiftStage
        {
            damageMultiplier = damageMultiplier,
            speedMultiplier = speedMultiplier,
        };
    }
}
