using System;
using System.Collections.Generic;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Grid;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Modules.Zones
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
                var gridManager = ServiceLocator.Get<GridManager>();
                if (gridManager != null)
                {
                    _objectivePosition = gridManager.ObjectivePosition;
                    _objectiveCached = true;
                }
            }

            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable last = null;
            float furthestFromObjective = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                if (SharedOverlapBuffer[i].GetComponentInParent<EnemyController>() == null) continue;
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
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
