using System;

namespace Gridlock.Mods.Pipeline
{
    [Flags]
    public enum ModTags
    {
        None    = 0,
        Homing  = 1 << 0,
        Pierce  = 1 << 1,
        Bounce  = 1 << 2,
        Split   = 1 << 3,
        Heavy   = 1 << 4,
        Swift   = 1 << 5,
        Wide    = 1 << 6,
        Burn    = 1 << 7,
        Frost   = 1 << 8,
        Shock   = 1 << 9,
        Void    = 1 << 10,
        Leech   = 1 << 11,
    }

    public static class ModTagsUtil
    {
        public static ModTags FromModType(ModType type)
        {
            return type switch
            {
                ModType.Homing => ModTags.Homing,
                ModType.Pierce => ModTags.Pierce,
                ModType.Bounce => ModTags.Bounce,
                ModType.Split  => ModTags.Split,
                ModType.Heavy  => ModTags.Heavy,
                ModType.Swift  => ModTags.Swift,
                ModType.Wide   => ModTags.Wide,
                ModType.Burn   => ModTags.Burn,
                ModType.Frost  => ModTags.Frost,
                ModType.Shock  => ModTags.Shock,
                ModType.Void   => ModTags.Void,
                ModType.Leech  => ModTags.Leech,
                _ => ModTags.None,
            };
        }

        public static Raylib_cs.Color GetColor(ModTags tags)
        {
            if (tags.HasFlag(ModTags.Burn))  return new Raylib_cs.Color(255, 77, 13, 255);
            if (tags.HasFlag(ModTags.Frost)) return new Raylib_cs.Color(51, 153, 255, 255);
            if (tags.HasFlag(ModTags.Shock)) return new Raylib_cs.Color(255, 242, 51, 255);
            if (tags.HasFlag(ModTags.Void))  return new Raylib_cs.Color(153, 26, 255, 255);
            if (tags.HasFlag(ModTags.Leech)) return new Raylib_cs.Color(26, 255, 102, 255);
            if (tags.HasFlag(ModTags.Heavy)) return new Raylib_cs.Color(255, 80, 80, 255);
            if (tags.HasFlag(ModTags.Swift)) return new Raylib_cs.Color(100, 255, 180, 255);
            return new Raylib_cs.Color(0, 255, 255, 255);
        }
    }
}
