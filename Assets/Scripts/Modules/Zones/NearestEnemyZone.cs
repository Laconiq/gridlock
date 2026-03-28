using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class NearestEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (SharedOverlapBuffer[i].GetComponentInParent<EnemyController>() == null) continue;
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
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

        public override ZoneInstance CreateInstance()
        {
            return new NearestEnemyZone { cooldown = cooldown };
        }
    }
}
