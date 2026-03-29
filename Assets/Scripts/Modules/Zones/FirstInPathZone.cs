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
            var result = new List<ITargetable>(1);

            if (!_objectiveCached)
            {
                var gridManager = ServiceLocator.Get<GridManager>();
                if (gridManager != null)
                {
                    _objectivePosition = gridManager.ObjectivePosition;
                    _objectiveCached = true;
                }
            }

            float rangeSqr = range * range;

            ITargetable first = null;
            float closestToObjective = float.MaxValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.Controller.IsAlive) continue;

                float distSqr = (entry.Controller.Position - origin).sqrMagnitude;
                if (distSqr > rangeSqr) continue;

                float distToObj = Vector3.Distance(entry.Controller.Position, _objectivePosition);
                if (distToObj < closestToObjective)
                {
                    closestToObjective = distToObj;
                    first = entry.Controller;
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
