using System;
using System.Collections.Generic;
using AIWE.Enemies;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class LowestHealthPercentZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var colliders = Physics.OverlapSphere(origin, range);

            ITargetable lowest = null;
            float lowestPercent = float.MaxValue;

            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var health = col.GetComponentInParent<EnemyHealth>();
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
