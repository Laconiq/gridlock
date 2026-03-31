using System;
using Gridlock.Enemies;
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
            if (ctx.Target == null || !ctx.Target.IsAlive)
            {
                ctx.Target = FindNearest(ref ctx);
                if (ctx.Target == null) return;
            }

            var toTarget = ctx.Target.Position - ctx.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) return;

            var desired = toTarget.normalized;
            bool isMissile = ctx.Synergies != null && ctx.Synergies.Contains(SynergyEffect.Missile);
            ctx.Direction = isMissile ? desired : Vector3.Lerp(ctx.Direction, desired, turnSpeed * Time.deltaTime).normalized;
        }

        private static Interfaces.ITargetable FindNearest(ref ModContext ctx)
        {
            float bestDist = float.MaxValue;
            Interfaces.ITargetable best = null;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive) continue;
                if (ctx.HitInstances != null && ctx.HitInstances.Contains(entry.Controller.gameObject.GetEntityId()))
                    continue;

                var diff = entry.Controller.Position - ctx.Position;
                float dist = diff.x * diff.x + diff.z * diff.z;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = entry.Controller;
                }
            }

            return best;
        }

        public IModStage Clone() => new HomingStage { turnSpeed = turnSpeed };
    }
}
