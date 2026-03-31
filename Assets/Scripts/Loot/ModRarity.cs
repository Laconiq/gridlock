using UnityEngine;
using Gridlock.Mods;
using Gridlock.UI;

namespace Gridlock.Loot
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public static class ModRarity
    {
        public static Rarity GetRarity(ModType type) => type switch
        {
            // Common — core behaviors + basic elements
            ModType.Homing => Rarity.Common,
            ModType.Pierce => Rarity.Common,
            ModType.Bounce => Rarity.Common,
            ModType.Heavy  => Rarity.Common,
            ModType.Swift  => Rarity.Common,
            ModType.Wide   => Rarity.Common,
            ModType.Burn   => Rarity.Common,
            ModType.Frost  => Rarity.Common,

            // Uncommon — advanced traits + basic events
            ModType.Split  => Rarity.Uncommon,
            ModType.Shock  => Rarity.Uncommon,
            ModType.Void   => Rarity.Uncommon,
            ModType.Leech  => Rarity.Uncommon,
            ModType.OnHit  => Rarity.Uncommon,
            ModType.OnKill => Rarity.Uncommon,
            ModType.OnEnd  => Rarity.Uncommon,

            // Rare — temporal events
            ModType.OnDelay  => Rarity.Rare,
            ModType.OnPulse  => Rarity.Rare,

            // Epic — conditionals + meta
            ModType.OnOverkill => Rarity.Epic,
            ModType.IfBurning  => Rarity.Epic,
            ModType.IfFrozen   => Rarity.Epic,
            ModType.IfLow      => Rarity.Epic,

            _ => Rarity.Common
        };

        public static Color GetRarityColor(Rarity rarity) => rarity switch
        {
            Rarity.Common   => DesignConstants.RarityCommon,
            Rarity.Uncommon => DesignConstants.RarityUncommon,
            Rarity.Rare     => DesignConstants.RarityRare,
            Rarity.Epic     => DesignConstants.RarityEpic,
            _               => Color.white
        };

        public static float GetEmissionIntensity(Rarity rarity) => rarity switch
        {
            Rarity.Common   => 3f,
            Rarity.Uncommon => 5f,
            Rarity.Rare     => 8f,
            Rarity.Epic     => 12f,
            _               => 3f
        };

        public static float GetDropWeight(Rarity rarity) => rarity switch
        {
            Rarity.Common   => 50f,
            Rarity.Uncommon => 30f,
            Rarity.Rare     => 15f,
            Rarity.Epic     => 5f,
            _               => 50f
        };
    }
}
