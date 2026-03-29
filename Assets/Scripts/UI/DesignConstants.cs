using UnityEngine;

namespace Gridlock.UI
{
    public static class DesignConstants
    {
        // === Module category colors ===
        public static readonly Color Trigger = new(1f, 0.47f, 0.28f);        // #ff7948
        public static readonly Color Target = new(0.56f, 0.96f, 1f);         // #8ff5ff
        public static readonly Color Effect = new(0.18f, 0.97f, 0f);         // #2ff801

        // === Port colors ===
        public static readonly Color PortChain = new(0.56f, 0.96f, 1f);      // #8ff5ff (cyan)
        public static readonly Color PortEffect = new(0.18f, 0.97f, 0f);     // #2ff801 (green)

        // === Surface colors ===
        public static readonly Color Primary = new(0.56f, 0.96f, 1f);        // #8ff5ff
        public static readonly Color PrimaryDim = new(0f, 0.87f, 0.93f);     // #00deec
        public static readonly Color Secondary = new(0.18f, 0.97f, 0f);      // #2ff801
        public static readonly Color Tertiary = new(1f, 0.47f, 0.28f);       // #ff7948
        public static readonly Color Error = new(1f, 0.44f, 0.42f);          // #ff716c

        // === Text colors ===
        public static readonly Color OnSurface = Color.white;
        public static readonly Color OnSurfaceVariant = new(0.68f, 0.67f, 0.67f);  // #adaaab
        public static readonly Color Outline = new(0.46f, 0.46f, 0.46f);           // #767576

        // === Hex strings (for ColorUtility.TryParseHtmlString) ===
        public const string HexTrigger = "#ff7948";
        public const string HexTarget = "#8ff5ff";
        public const string HexEffect = "#2ff801";
        public const string HexPrimary = "#00F0FF";
        public const string HexError = "#ff716c";

        // === Node layout (px, matches USS) ===
        public const float NodeWidth = 192f;
        public const float SidePortY = 77f;       // center of side port from node top
        public const float NodeHeight = 94f;       // height without status bar
        public const float EffectNodeHeight = 114f;// with status bar

        // === Icon resource paths ===
        public const string IconTrigger = "UI/NodeEditor/Icons/icon_trigger";
        public const string IconTarget = "UI/NodeEditor/Icons/icon_zone";
        public const string IconEffect = "UI/NodeEditor/Icons/icon_effect";
        public const string IconHelp = "UI/NodeEditor/Icons/icon_help";

        public static string GetIconPath(string cssClass) => cssClass switch
        {
            "trigger" => IconTrigger,
            "zone" => IconTarget,
            "effect" => IconEffect,
            _ => null
        };

    }
}
