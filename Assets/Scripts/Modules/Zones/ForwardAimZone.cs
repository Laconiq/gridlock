using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class ForwardAimZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            if (Owner?.FirePoint == null) return result;

            var aimPoint = origin + Owner.FirePoint.forward * range;
            result.Add(new AimTarget(aimPoint, Owner.FirePoint));
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new ForwardAimZone { cooldown = cooldown };
        }

        private class AimTarget : ITargetable
        {
            public Vector3 Position { get; }
            public bool IsAlive => true;
            public Transform Transform { get; }

            public AimTarget(Vector3 position, Transform firePoint)
            {
                Position = position;
                Transform = firePoint;
            }
        }
    }
}
