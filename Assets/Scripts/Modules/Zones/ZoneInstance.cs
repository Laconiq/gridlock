using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public abstract class ZoneInstance
    {
        [NonSerialized] public ZoneDefinition Definition;
        [NonSerialized] public IChassis Owner;

        public abstract List<ITargetable> SelectTargets(Vector3 origin, float range);

        public abstract ZoneInstance CreateInstance();
    }
}
