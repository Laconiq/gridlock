using System;
using Gridlock.Combat;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class ShockStage : IModStage
    {
        [SerializeField] private int chainCount = 1;
        [SerializeField] private float chainRadius = 3f;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            var origin = ctx.Position;
            int chained = 0;

            for (int c = 0; c < chainCount && c < EnemyRegistry.Count; c++)
            {
                float bestDist = float.MaxValue;
                EnemyEntry best = default;
                bool found = false;

                foreach (var entry in EnemyRegistry.All)
                {
                    if (!entry.Health.IsAlive) continue;
                    var id = entry.Controller.gameObject.GetEntityId();
                    if (ctx.HitInstances.Contains(id)) continue;

                    var delta = entry.Controller.transform.position - origin;
                    float dist = new Vector2(delta.x, delta.z).magnitude;
                    if (dist > chainRadius || dist >= bestDist) continue;

                    bestDist = dist;
                    best = entry;
                    found = true;
                }

                if (!found) break;

                ctx.HitInstances.Add(best.Controller.gameObject.GetEntityId());
                best.Health.TakeDamage(new DamageInfo(ctx.Damage, DamageType.Direct));
                origin = best.Controller.transform.position;
                chained++;
            }
        }

        public IModStage Clone() => new ShockStage
        {
            chainCount = chainCount,
            chainRadius = chainRadius
        };
    }
}
