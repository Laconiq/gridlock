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
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            for (int i = 0; i < count; i++)
            {
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
