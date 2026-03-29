using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Mods
{
    [Serializable]
    public class ModSlotData
    {
        public ModType modType;
    }

    [Serializable]
    public class ModChainData
    {
        public List<ModSlotData> slots = new();
        public TargetingMode targetingMode = TargetingMode.First;
    }
}
