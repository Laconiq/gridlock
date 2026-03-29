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
            ObjectiveController.Instance?.Heal(ctx.Damage * leechPercent);
        }

        public IModStage Clone() => new LeechStage { leechPercent = leechPercent };
    }
}
