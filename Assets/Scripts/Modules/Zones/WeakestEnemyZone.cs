using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class WeakestEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>(1);
            float rangeSqr = range * range;

            ITargetable weakest = null;
            float lowestHP = float.MaxValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive || entry.Health == null) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                if (entry.Health.CurrentHP < lowestHP)
                {
                    lowestHP = entry.Health.CurrentHP;
                    weakest = entry.Controller;
                }
            }

            if (weakest != null) result.Add(weakest);
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new WeakestEnemyZone { cooldown = cooldown };
        }
    }
}
