using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class SplitStage : IModStage
    {
        [SerializeField] private float arcDegrees = 90f;

        public int ExtraCount { get; set; } = 1;
        public int BarrageBonus { get; set; }

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            int total = 1 + ExtraCount + BarrageBonus;

            var baseDir = ctx.Direction;
            if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;

            float startAngle = total > 1 ? -arcDegrees * 0.5f : 0f;
            float step = total > 1 ? arcDegrees / (total - 1) : 0f;

            for (int i = 0; i < total; i++)
            {
                float angle = startAngle + step * i;
                var dir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
                ctx.SpawnRequests.Add(new SpawnRequest
                {
                    Origin = ctx.Position,
                    Direction = dir,
                    Pipeline = ctx.OwnerPipeline?.CloneExcludingPhase(StagePhase.Configure),
                    DamageScale = 1f,
                    Target = ctx.Target,
                });
            }

            ctx.Consumed = true;
        }

        public IModStage Clone() => new SplitStage
        {
            arcDegrees = arcDegrees,
            ExtraCount = ExtraCount,
            BarrageBonus = BarrageBonus
        };
    }
}
