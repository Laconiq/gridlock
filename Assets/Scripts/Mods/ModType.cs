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

        // Events - mod-dependent
        OnPierce,
        OnBounce,
        OnChain,

        // Events - temporal
        OnDelay,
        OnPulse,

        // Events - conditional
        IfBurning,
        IfFrozen,
        IfShocked,
        IfLow,

        // Events - meta/rare
        OnCrit,
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

        public static bool IsModDependent(this ModType type)
        {
            return type >= ModType.OnPierce && type <= ModType.OnChain;
        }

        public static bool IsConditional(this ModType type)
        {
            return type >= ModType.IfBurning && type <= ModType.IfLow;
        }

        public static ModType? RequiredMod(this ModType eventType)
        {
            return eventType switch
            {
                ModType.OnPierce => ModType.Pierce,
                ModType.OnBounce => ModType.Bounce,
                ModType.OnChain => ModType.Shock,
                _ => null
            };
        }
    }
}
