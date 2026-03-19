using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    public abstract class ZoneInstance
    {
        public ZoneDefinition Definition { get; set; }
        public IChassis Owner { get; set; }

        public abstract List<ITargetable> SelectTargets(Vector3 origin, float range);
    }
}
