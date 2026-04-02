using Gridlock.Grid;

namespace Gridlock.Visual
{
    public struct WarpFollower
    {
        public float BaseY;
        public float Influence;

        public WarpFollower(float baseY, float influence = 1f)
        {
            BaseY = baseY;
            Influence = influence;
        }

        public float GetWarpedY(float worldX, float worldZ)
        {
            var warp = GridWarpManager.Instance;
            if (warp == null) return BaseY;
            return BaseY + warp.GetWarpOffset(worldX, worldZ) * Influence;
        }
    }
}
