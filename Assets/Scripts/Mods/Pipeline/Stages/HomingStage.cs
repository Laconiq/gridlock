using System;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class HomingStage : IModStage
    {
        [SerializeField] private float turnSpeed = 8f;

        public StagePhase Phase => StagePhase.OnUpdate;

        public void Execute(ref ModContext ctx)
        {
            if (ctx.Target == null || !ctx.Target.IsAlive) return;

            var toTarget = ctx.Target.Position - ctx.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) return;

            var desired = toTarget.normalized;
            bool isMissile = (ctx.Tags & (ModTags.Homing | ModTags.Swift)) == (ModTags.Homing | ModTags.Swift);
            ctx.Direction = isMissile ? desired : Vector3.Lerp(ctx.Direction, desired, turnSpeed * Time.deltaTime).normalized;
        }

        public IModStage Clone() => new HomingStage { turnSpeed = turnSpeed };
    }
}
