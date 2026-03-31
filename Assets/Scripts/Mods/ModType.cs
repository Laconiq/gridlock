namespace Gridlock.Mods
{
    public enum ModType
    {
        // Behavior traits
        Homing,
        Pierce,
        Bounce,
        Split,
        Heavy,
        Swift,
        Wide,

        // Elemental traits
        Burn,
        Frost,
        Shock,
        Void,
        Leech,

        // Events - basic
        OnHit,
        OnKill,
        OnEnd,

        // Events - temporal
        OnDelay,
        OnPulse,

        // Events - conditional
        IfBurning,
        IfFrozen,
        IfLow,

        // Events - meta
        OnOverkill,
    }

    public static class ModTypeExtensions
    {
        public static bool IsEvent(this ModType type)
        {
            return type >= ModType.OnHit;
        }

        public static bool IsTrait(this ModType type)
        {
            return type < ModType.OnHit;
        }

        public static bool IsElemental(this ModType type)
        {
            return type >= ModType.Burn && type <= ModType.Leech;
        }

        public static bool IsConditional(this ModType type)
        {
            return type >= ModType.IfBurning && type <= ModType.IfLow;
        }
    }
}
