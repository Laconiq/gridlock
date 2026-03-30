using UnityEngine;

namespace Gridlock.UI
{
    public static class DesignConstants
    {
        public static readonly Color Primary = new(0.56f, 0.96f, 1f);        // #8ff5ff
        public static readonly Color Secondary = new(0.18f, 0.97f, 0f);      // #2ff801
        public static readonly Color Tertiary = new(1f, 0.47f, 0.28f);       // #ff7948
        public static readonly Color Outline = new(0.46f, 0.46f, 0.46f);     // #767576

        // Rarity
        public static readonly Color RarityCommon = Color.white;
        public static readonly Color RarityUncommon = new(0.56f, 0.96f, 1f);   // #8ff5ff
        public static readonly Color RarityRare = new(1f, 0.24f, 0.86f);       // #ff3edc
        public static readonly Color RarityEpic = new(1f, 0.47f, 0.28f);       // #ff7948
    }
}
