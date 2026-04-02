using System;
using System.Numerics;
using Gridlock.Towers;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class TowerVisual
    {
        private static readonly Color BaseColor = new(0, 200, 255, 255);
        private static readonly Color TurretColor = new(0, 240, 255, 255);

        private const float BaseSize = 0.8f;
        private const float TurretRadius = 0.2f;
        private const float BobAmplitude = 0.05f;
        private const float BobFrequency = 2f;
        private const float IdleSpinSpeed = 1.5f;

        private float _time;

        public void Render(Tower tower, float dt)
        {
            _time += dt;

            var pos = tower.Position;
            float warpY = 0f;
            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warpY = warp.GetWarpOffset(pos.X, pos.Z);

            var basePos = new System.Numerics.Vector3(pos.X, warpY + BaseSize * 0.5f, pos.Z);
            var baseSize = new System.Numerics.Vector3(BaseSize, BaseSize, BaseSize);
            Raylib.DrawCubeWiresV(basePos, baseSize, BaseColor);

            float bob = MathF.Sin(_time * BobFrequency * MathF.Tau) * BobAmplitude;
            var turretPos = new System.Numerics.Vector3(
                pos.X,
                warpY + BaseSize + TurretRadius + 0.1f + bob,
                pos.Z);
            Raylib.DrawSphere(turretPos, TurretRadius, TurretColor);
        }
    }
}
