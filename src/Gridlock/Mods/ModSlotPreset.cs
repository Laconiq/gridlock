using System.Collections.Generic;

namespace Gridlock.Mods
{
    public sealed class ModSlotPreset
    {
        public TargetingMode TargetingMode { get; set; } = TargetingMode.First;
        public List<ModType> Slots { get; set; } = new();
    }
}
