using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class AllEnemiesInRangeZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var seen = new HashSet<ITargetable>();
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            for (int i = 0; i < count; i++)
            {
                if (SharedOverlapBuffer[i].GetComponentInParent<EnemyController>() == null) continue;
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
                if (target != null && target.IsAlive && seen.Add(target))
                {
                    result.Add(target);
                }
            }

            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new AllEnemiesInRangeZone { cooldown = cooldown };
        }
    }
}
