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
            var result = new List<ITargetable>();
            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable weakest = null;
            float lowestHP = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var health = SharedOverlapBuffer[i].GetComponentInParent<EnemyHealth>();
                if (health == null) continue;

                if (health.CurrentHP < lowestHP)
                {
                    lowestHP = health.CurrentHP;
                    weakest = target;
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
