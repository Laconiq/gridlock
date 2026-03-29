using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class HighestHealthPercentZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>(1);
            float rangeSqr = range * range;

            ITargetable highest = null;
            float highestPercent = float.MinValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive || entry.Health == null || entry.Health.MaxHP <= 0f) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                float percent = entry.Health.CurrentHP / entry.Health.MaxHP;
                if (percent > highestPercent)
                {
                    highestPercent = percent;
                    highest = entry.Controller;
                }
            }

            if (highest != null) result.Add(highest);
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new HighestHealthPercentZone { cooldown = cooldown };
        }
    }
}
