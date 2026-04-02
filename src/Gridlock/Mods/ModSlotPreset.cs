using System.Collections.Generic;

namespace Gridlock.Mods
{
    public sealed class ModSlotPreset
    {
        public string PresetName { get; set; } = "";
        public string Description { get; set; } = "";
        public TargetingMode TargetingMode { get; set; } = TargetingMode.First;
        public List<ModType> Slots { get; set; } = new();
    }
}
