using System;
using UnityEngine;

namespace Gridlock.Mods
{
    [Serializable]
    public struct SynergyDef
    {
        public ModType modA;
        public ModType modB;
        public string synergyName;
        public SynergyEffect effect;
    }

    public enum SynergyEffect
    {
        Railgun,       // Heavy+Heavy → free Pierce
        Machinegun,    // Swift+Swift → double fire rate
        Blizzard,      // Frost+Frost → stun instead of slow
        Tesla,         // Shock+Shock → chain 3 instead of 1
        Napalm,        // Burn+Wide → TODO: requires zone system
        Avalanche,     // Frost+Wide → TODO: requires zone system
        Missile,       // Homing+Swift → perfect homing
        Meteor,        // Heavy+Wide → 2x AOE radius
        Siphon,        // Shock+Void → void heals objective
        Barrage,       // Split+Split → x5 instead of x3+x3
        Vampire,       // Leech+Heavy → 40% drain
    }

    public static class SynergyTable
    {
        private static readonly SynergyDef[] _synergies =
        {
            new() { modA = ModType.Heavy, modB = ModType.Heavy, synergyName = "RAILGUN", effect = SynergyEffect.Railgun },
            new() { modA = ModType.Swift, modB = ModType.Swift, synergyName = "MACHINEGUN", effect = SynergyEffect.Machinegun },
            new() { modA = ModType.Frost, modB = ModType.Frost, synergyName = "BLIZZARD", effect = SynergyEffect.Blizzard },
            new() { modA = ModType.Shock, modB = ModType.Shock, synergyName = "TESLA", effect = SynergyEffect.Tesla },
            new() { modA = ModType.Burn, modB = ModType.Wide, synergyName = "NAPALM", effect = SynergyEffect.Napalm },
            new() { modA = ModType.Frost, modB = ModType.Wide, synergyName = "AVALANCHE", effect = SynergyEffect.Avalanche },
            new() { modA = ModType.Homing, modB = ModType.Swift, synergyName = "MISSILE", effect = SynergyEffect.Missile },
            new() { modA = ModType.Heavy, modB = ModType.Wide, synergyName = "METEOR", effect = SynergyEffect.Meteor },
            new() { modA = ModType.Shock, modB = ModType.Void, synergyName = "SIPHON", effect = SynergyEffect.Siphon },
            new() { modA = ModType.Split, modB = ModType.Split, synergyName = "BARRAGE", effect = SynergyEffect.Barrage },
            new() { modA = ModType.Leech, modB = ModType.Heavy, synergyName = "VAMPIRE", effect = SynergyEffect.Vampire },
        };

        public static SynergyDef? Check(ModType a, ModType b)
        {
            foreach (var syn in _synergies)
            {
                if ((syn.modA == a && syn.modB == b) || (syn.modA == b && syn.modB == a))
                    return syn;
            }
            return null;
        }

        public static SynergyDef[] All => _synergies;
    }
}
