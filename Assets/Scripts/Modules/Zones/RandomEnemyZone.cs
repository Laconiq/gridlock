using System;
using System.Collections.Generic;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
{
    [Serializable]
    public class RandomEnemyZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>(1);
            float rangeSqr = range * range;

            SharedResultBuffer.Clear();

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr <= rangeSqr)
                    SharedResultBuffer.Add(entry.Controller);
            }

            if (SharedResultBuffer.Count > 0)
                result.Add(SharedResultBuffer[UnityEngine.Random.Range(0, SharedResultBuffer.Count)]);

            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new RandomEnemyZone { cooldown = cooldown };
        }
    }
}
