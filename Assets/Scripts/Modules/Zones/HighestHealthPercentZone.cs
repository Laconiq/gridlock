using System;
using System.Collections.Generic;
using AIWE.Enemies;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class HighestHealthPercentZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var colliders = Physics.OverlapSphere(origin, range);

            ITargetable highest = null;
            float highestPercent = float.MinValue;

            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var health = col.GetComponentInParent<EnemyHealth>();
                if (health == null || health.MaxHP <= 0f) continue;

                float percent = health.CurrentHP / health.MaxHP;
                if (percent > highestPercent)
                {
                    highestPercent = percent;
                    highest = target;
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
