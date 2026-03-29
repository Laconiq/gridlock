using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Mods
{
    [CreateAssetMenu(menuName = "Gridlock/Mod Slot Preset")]
    public class ModSlotPreset : ScriptableObject
    {
        public string presetName;
        public string description;
        public TargetingMode targetingMode = TargetingMode.First;
        public List<ModType> slots = new();
    }
}
