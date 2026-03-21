using System;
using System.Collections.Generic;
using AIWE.Enemies;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class StrongestEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable strongest = null;
            float highestHP = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var health = SharedOverlapBuffer[i].GetComponentInParent<EnemyHealth>();
                if (health == null) continue;

                if (health.CurrentHP > highestHP)
                {
                    highestHP = health.CurrentHP;
                    strongest = target;
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
