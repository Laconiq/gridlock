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
            var result = new List<ITargetable>(1);
            float rangeSqr = range * range;

            ITargetable nearest = null;
            float nearestDistSqr = float.MaxValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr <= rangeSqr && distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = entry.Controller;
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
