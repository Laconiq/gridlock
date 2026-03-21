using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class RandomEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var colliders = Physics.OverlapSphere(origin, range);

            var candidates = new List<ITargetable>();

            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;
                if (!candidates.Contains(target)) candidates.Add(target);
            }

            if (candidates.Count > 0)
                result.Add(candidates[UnityEngine.Random.Range(0, candidates.Count)]);

            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new RandomEnemyZone { cooldown = cooldown };
        }
    }
}
