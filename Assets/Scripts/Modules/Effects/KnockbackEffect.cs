using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class KnockbackEffect : EffectInstance
    {
        [SerializeField] private float force = 5f;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var direction = (target.Position - origin).normalized;
                var newPos = target.Transform.position + direction * force;

                var agent = target.Transform.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(newPos, out var hit, force, NavMesh.AllAreas))
                        agent.Warp(hit.position);
                }
                else
                {
                    target.Transform.position = newPos;
                }
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new KnockbackEffect { force = force, cooldown = cooldown };
        }
    }
}
