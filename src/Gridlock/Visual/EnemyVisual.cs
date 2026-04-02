using System;
using System.Numerics;
using Gridlock.Enemies;
using Raylib_cs;

namespace Gridlock.Visual
{
    public sealed class EnemyVisual
    {
        private const float FlashDuration = 0.1f;
        private static readonly Color FlashColor = new(255, 255, 255, 255);

        private readonly float[] _flashTimers;
        private readonly int _capacity;

        public EnemyVisual(int capacity = 256)
        {
            _capacity = capacity;
            _flashTimers = new float[capacity];
        }

        public void OnHit(int entityId)
        {
            if (entityId >= 0 && entityId < _capacity)
                _flashTimers[entityId] = FlashDuration;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (_flashTimers[i] > 0f)
                    _flashTimers[i] = MathF.Max(0f, _flashTimers[i] - dt);
            }
        }

        public void Render(Enemy enemy)
        {
            if (!enemy.IsAlive) return;

            var pos = enemy.Position;
            float warpY = 0f;
            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warpY = warp.GetWarpOffset(pos.X, pos.Z);

            var scale = enemy.Data.Scale;
            var drawPos = new System.Numerics.Vector3(pos.X, pos.Y + warpY, pos.Z);
            var drawSize = new System.Numerics.Vector3(scale.X * 0.5f, scale.Y * 0.5f, scale.Z * 0.5f);

            uint cVal = enemy.Data.Color;
            byte baseR = (byte)((cVal >> 24) & 0xFF);
            byte baseG = (byte)((cVal >> 16) & 0xFF);
            byte baseB = (byte)((cVal >> 8) & 0xFF);
            byte baseA = (byte)(cVal & 0xFF);
            var baseColor = new Color(baseR, baseG, baseB, baseA);

            bool flashing = enemy.EntityId < _capacity && _flashTimers[enemy.EntityId] > 0f;

            Color color;
            if (flashing)
            {
                float t = 1f - _flashTimers[enemy.EntityId] / FlashDuration;
                byte lr = (byte)(255 + (baseR - 255) * t);
                byte lg = (byte)(255 + (baseG - 255) * t);
                byte lb = (byte)(255 + (baseB - 255) * t);
                color = new Color(lr, lg, lb, baseA);
            }
            else
            {
                color = baseColor;
            }

            Raylib.DrawCubeV(drawPos, drawSize, color);
            Raylib.DrawCubeWiresV(drawPos, drawSize, new Color(255, 255, 255, 60));
        }

        public void Clear()
        {
            Array.Clear(_flashTimers, 0, _capacity);
        }
    }
}
