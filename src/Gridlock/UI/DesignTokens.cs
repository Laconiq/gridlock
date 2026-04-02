using Raylib_cs;

namespace Gridlock.UI
{
    public static class DesignTokens
    {
        // Surfaces
        public static readonly Color Surface = new(14, 14, 15, 255);
        public static readonly Color SurfaceDim = new(14, 14, 15, 255);
        public static readonly Color SurfaceContainerLowest = new(0, 0, 0, 255);
        public static readonly Color SurfaceContainerLow = new(19, 19, 20, 255);
        public static readonly Color SurfaceContainer = new(25, 25, 27, 255);
        public static readonly Color SurfaceContainerHigh = new(32, 31, 33, 255);
        public static readonly Color SurfaceContainerHighest = new(38, 38, 39, 255);
        public static readonly Color SurfaceBright = new(44, 44, 45, 255);

        // Primary (Cyan)
        public static readonly Color Primary = new(143, 245, 255, 255);
        public static readonly Color PrimaryDim = new(0, 222, 236, 255);
        public static readonly Color PrimaryContainer = new(0, 238, 252, 255);
        public static readonly Color OnPrimary = new(0, 93, 99, 255);

        // Secondary (Green)
        public static readonly Color Secondary = new(47, 248, 1, 255);
        public static readonly Color SecondaryDim = new(43, 232, 0, 255);
        public static readonly Color SecondaryContainer = new(16, 110, 0, 255);

        // Tertiary (Orange)
        public static readonly Color Tertiary = new(255, 121, 72, 255);
        public static readonly Color TertiaryDim = new(255, 116, 65, 255);
        public static readonly Color TertiaryContainer = new(254, 94, 30, 255);

        // Error (Red)
        public static readonly Color Error = new(255, 113, 108, 255);
        public static readonly Color ErrorContainer = new(159, 5, 25, 255);

        // On-Colors
        public static readonly Color OnSurface = new(255, 255, 255, 255);
        public static readonly Color OnSurfaceVariant = new(173, 170, 171, 255);
        public static readonly Color Outline = new(118, 117, 118, 255);
        public static readonly Color OutlineVariant = new(72, 72, 73, 255);

        // Mod Categories
        public static readonly Color ColorTrigger = new(255, 121, 72, 255);
        public static readonly Color ColorTarget = new(143, 245, 255, 255);
        public static readonly Color ColorEffect = new(47, 248, 1, 255);

        // Glass / Blur
        public static readonly Color GlassBorder = new(0, 238, 252, 38);
        public static readonly Color GlassBorderAccent = new(0, 238, 252, 77);
        public static readonly Color GlassInnerBg = new(14, 16, 20, 115);
        public static readonly Color GlassInnerBgHover = new(30, 30, 35, 128);

        // Rarity
        public static readonly Color RarityCommon = new(255, 255, 255, 255);
        public static readonly Color RarityUncommon = new(143, 245, 255, 255);
        public static readonly Color RarityRare = new(255, 62, 220, 255);
        public static readonly Color RarityEpic = new(255, 121, 72, 255);

        // Status
        public static readonly Color Success = new(47, 248, 1, 255);

        // Elemental
        public static readonly Color ElementBurn = new(255, 120, 30, 255);
        public static readonly Color ElementFrost = new(100, 200, 255, 255);
        public static readonly Color ElementShock = new(255, 255, 100, 255);
        public static readonly Color ElementVoid = new(160, 50, 255, 255);
        public static readonly Color ElementLeech = new(200, 40, 120, 255);

        // HUD
        public static readonly Color HudHpColor = new(0, 238, 252, 255);
        public static readonly Color HudHpLow = new(255, 121, 72, 255);
        public static readonly Color HudHpCritical = new(255, 113, 108, 255);
        public static readonly Color HudHpTrack = new(0, 238, 252, 31);
        public static readonly Color HudCornerColor = new(0, 238, 252, 38);
        public static readonly Color HudSquadConnected = new(47, 248, 1, 255);
        public static readonly Color HudSquadDisconnected = new(255, 113, 108, 255);

        // Spacing
        public const int SpaceXs = 4;
        public const int SpaceSm = 8;
        public const int SpaceMd = 12;
        public const int SpaceLg = 16;
        public const int SpaceXl = 24;
        public const int Space2Xl = 32;

        // Font sizes
        public const int FontSizeXs = 8;
        public const int FontSizeSm = 9;
        public const int FontSizeBody = 11;
        public const int FontSizeMd = 13;
        public const int FontSizeLg = 18;
        public const int FontSizeXl = 22;
    }
}
