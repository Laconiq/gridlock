using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class StrongestEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>(1);
            float rangeSqr = range * range;

            ITargetable strongest = null;
            float highestHP = float.MinValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive || entry.Health == null) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                if (entry.Health.CurrentHP > highestHP)
                {
                    highestHP = entry.Health.CurrentHP;
                    strongest = entry.Controller;
                }
            }

            if (strongest != null) result.Add(strongest);
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new StrongestEnemyZone { cooldown = cooldown };
        }
    }
}
