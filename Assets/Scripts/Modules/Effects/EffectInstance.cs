using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public abstract class EffectInstance
    {
        [NonSerialized] public EffectDefinition Definition;
        [NonSerialized] public IChassis Owner;

        public abstract void Execute(List<ITargetable> targets, Vector3 origin);

        public abstract EffectInstance CreateInstance();
    }
}
