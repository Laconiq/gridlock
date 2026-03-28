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
    public class FirstInPathZone : ZoneInstance
    {
        [NonSerialized] private Vector3 _objectivePosition;
        [NonSerialized] private bool _objectiveCached;

        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();

            if (!_objectiveCached)
            {
                var gridManager = ServiceLocator.Get<GridManager>();
                if (gridManager != null) _objectivePosition = gridManager.ObjectivePosition;
                _objectiveCached = true;
            }

            int count = Physics.OverlapSphereNonAlloc(origin, range, SharedOverlapBuffer);

            ITargetable first = null;
            float closestToObjective = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (SharedOverlapBuffer[i].GetComponentInParent<EnemyController>() == null) continue;
                var target = SharedOverlapBuffer[i].GetComponentInParent<ITargetable>();
                if (target == null || !target.IsAlive) continue;

                var dist = Vector3.Distance(target.Position, _objectivePosition);
                if (dist < closestToObjective)
                {
                    closestToObjective = dist;
                    first = target;
                }
            }

            if (first != null) result.Add(first);
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new FirstInPathZone { cooldown = cooldown };
        }
    }
}
