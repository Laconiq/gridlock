using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    public class NearestEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var colliders = Physics.OverlapSphere(origin, range);

            ITargetable nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var dist = Vector3.Distance(origin, target.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = target;
                }
            }

            if (nearest != null) result.Add(nearest);
            return result;
        }
    }
}
