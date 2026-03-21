using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class SelfTargetZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            if (Owner?.FirePoint == null) return result;

            var ownerTransform = Owner.FirePoint.root;
            result.Add(new SelfTarget(ownerTransform));
            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new SelfTargetZone { cooldown = cooldown };
        }

        private class SelfTarget : ITargetable
        {
            public Vector3 Position { get; }
            public bool IsAlive => true;
            public Transform Transform { get; }

            public SelfTarget(Transform t)
            {
                Position = t.position;
                Transform = t;
            }
        }
    }
}
