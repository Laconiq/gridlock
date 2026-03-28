using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class LowestHealthPercentZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable lowest = null;
            float lowestPercent = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var health = SharedOverlapBuffer[i].GetComponentInParent<EnemyHealth>();
                if (health == null || health.MaxHP <= 0f) continue;

                float percent = health.CurrentHP / health.MaxHP;
                if (percent < lowestPercent)
                {
                    lowestPercent = percent;
                    lowest = target;
                }
            }

            if (lowest != null) result.Add(lowest);
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new LowestHealthPercentZone { cooldown = cooldown };
        }
    }
}
