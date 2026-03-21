using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.LevelDesign;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class LastInPathZone : ZoneInstance
    {
        [NonSerialized] private Vector3 _objectivePosition;
        [NonSerialized] private bool _objectiveCached;

        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();

            if (!_objectiveCached)
            {
                var marker = UnityEngine.Object.FindAnyObjectByType<ObjectiveMarker>();
                if (marker != null) _objectivePosition = marker.transform.position;
                _objectiveCached = true;
            }

            var colliders = Physics.OverlapSphere(origin, range);

            ITargetable last = null;
            float furthestFromObjective = float.MinValue;

            foreach (var col in colliders)
            {
                var target = col.GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var dist = Vector3.Distance(target.Position, _objectivePosition);
                if (dist > furthestFromObjective)
                {
                    furthestFromObjective = dist;
                    last = target;
                }
            }

            if (last != null) result.Add(last);
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new LastInPathZone { cooldown = cooldown };
        }
    }
}
