using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    public abstract class EffectInstance
    {
        public EffectDefinition Definition { get; set; }
        public IChassis Owner { get; set; }

        public abstract void Execute(List<ITargetable> targets, Vector3 origin);
    }
}
