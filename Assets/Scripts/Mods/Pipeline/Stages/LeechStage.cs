using System;
using Gridlock.Core;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class LeechStage : IModStage
    {
        [SerializeField] private float leechPercent = 0.12f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            float percent = leechPercent;
            if (ctx.Synergies != null && ctx.Synergies.Contains(SynergyEffect.Vampire))
                percent = 0.4f;
            ObjectiveController.Instance?.Heal(ctx.Damage * percent);
        }

        public IModStage Clone() => new LeechStage { leechPercent = leechPercent };
    }
}
