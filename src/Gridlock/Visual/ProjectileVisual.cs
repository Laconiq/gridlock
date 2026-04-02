using System;
using System.Numerics;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Raylib_cs;

namespace Gridlock.Visual
{
    public static class ProjectileVisual
    {
        private static readonly Color BurnColor = new(255, 77, 13, 255);
        private static readonly Color FrostColor = new(51, 153, 255, 255);
        private static readonly Color ShockColor = new(255, 242, 51, 255);
        private static readonly Color VoidColor = new(153, 26, 255, 255);
        private static readonly Color LeechColor = new(26, 255, 102, 255);
        private static readonly Color DefaultColor = new(0, 255, 255, 255);

        public static Color GetElementColor(ModTags tags)
        {
            if (tags.HasFlag(ModTags.Burn)) return BurnColor;
            if (tags.HasFlag(ModTags.Frost)) return FrostColor;
            if (tags.HasFlag(ModTags.Shock)) return ShockColor;
            if (tags.HasFlag(ModTags.Void)) return VoidColor;
            if (tags.HasFlag(ModTags.Leech)) return LeechColor;
            return DefaultColor;
        }

        public static void Render(ModProjectile proj)
        {
            if (proj.IsDestroyed) return;

            var ctx = proj.Context;
            var color = GetElementColor(ctx.Tags);
            float radius = 0.1f + ctx.Size * 0.08f;

            var pos = ctx.Position;
            float warpY = 0f;
            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warpY = warp.GetWarpOffset(pos.X, pos.Z);

            var drawPos = new System.Numerics.Vector3(pos.X, pos.Y + warpY, pos.Z);

            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawSphere(drawPos, radius, color);
            Raylib.EndBlendMode();
        }
    }
}
