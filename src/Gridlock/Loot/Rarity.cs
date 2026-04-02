using Gridlock.Mods;

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
            ModType.Homing => Rarity.Common,
            ModType.Pierce => Rarity.Common,
            ModType.Bounce => Rarity.Common,
            ModType.Heavy  => Rarity.Common,
            ModType.Swift  => Rarity.Common,
            ModType.Wide   => Rarity.Common,
            ModType.Burn   => Rarity.Common,
            ModType.Frost  => Rarity.Common,

            ModType.Split  => Rarity.Uncommon,
            ModType.Shock  => Rarity.Uncommon,
            ModType.Void   => Rarity.Uncommon,
            ModType.Leech  => Rarity.Uncommon,
            ModType.OnHit  => Rarity.Uncommon,
            ModType.OnKill => Rarity.Uncommon,
            ModType.OnEnd  => Rarity.Uncommon,

            ModType.OnDelay  => Rarity.Rare,
            ModType.OnPulse  => Rarity.Rare,

            ModType.OnOverkill => Rarity.Epic,
            ModType.IfBurning  => Rarity.Epic,
            ModType.IfFrozen   => Rarity.Epic,
            ModType.IfLow      => Rarity.Epic,

            _ => Rarity.Common
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
