using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class AllEnemiesInRangeZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var seen = new HashSet<ITargetable>();
            var colliders = Physics.OverlapSphere(origin, range);

            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<ITargetable>();
                if (target != null && target.IsAlive && seen.Add(target))
                {
                    result.Add(target);
                }
            }

            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new AllEnemiesInRangeZone();
        }
    }
}
