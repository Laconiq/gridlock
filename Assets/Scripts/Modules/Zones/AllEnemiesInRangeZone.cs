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
            float rangeSqr = range * range;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr <= rangeSqr)
                    result.Add(entry.Controller);
            }

            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new AllEnemiesInRangeZone { cooldown = cooldown };
        }
    }
}
