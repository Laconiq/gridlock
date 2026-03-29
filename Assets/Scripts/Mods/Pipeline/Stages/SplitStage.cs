using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class SplitStage : IModStage
    {
        [SerializeField] private int splitCount = 3;
        [SerializeField] private float arcDegrees = 120f;

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            float startAngle = -arcDegrees * 0.5f;
            float step = splitCount > 1 ? arcDegrees / (splitCount - 1) : 0f;

            var baseDir = ctx.Direction;
            if (baseDir.sqrMagnitude < 0.001f) baseDir = Vector3.forward;

            for (int i = 0; i < splitCount; i++)
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


        public IModStage Clone() => new SplitStage { splitCount = splitCount, arcDegrees = arcDegrees };
    }
}
