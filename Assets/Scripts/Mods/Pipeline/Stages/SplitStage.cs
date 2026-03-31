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
            int extras = ExtraCount + BarrageBonus;
            if (extras <= 0) return;

            int total = 1 + extras;
            var baseDir = ctx.Direction;
            if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;

            float startAngle = -arcDegrees * 0.5f;
            float step = total > 1 ? arcDegrees / (total - 1) : 0f;

            ctx.Direction = Quaternion.AngleAxis(startAngle, Vector3.up) * baseDir;

            for (int i = 1; i < total; i++)
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
        }

        public IModStage Clone() => new SplitStage
        {
            arcDegrees = arcDegrees,
            ExtraCount = ExtraCount,
            BarrageBonus = BarrageBonus
        };
    }
}
