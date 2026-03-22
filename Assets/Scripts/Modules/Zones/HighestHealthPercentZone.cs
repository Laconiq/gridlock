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
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable highest = null;
            float highestPercent = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var health = SharedOverlapBuffer[i].GetComponentInParent<EnemyHealth>();
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
