using System.Collections.Generic;

namespace Gridlock.Mods
{
    public sealed class ModSlotData
    {
        public ModType modType;
    }

    public sealed class ModChainData
    {
        public List<ModSlotData> slots = new();
        public TargetingMode targetingMode = TargetingMode.First;
    }
}
