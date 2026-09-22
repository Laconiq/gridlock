using System;
using System.Numerics;
using Gridlock.Combat;

namespace Gridlock.Mods.Pipeline
{
    public struct SpawnRequest
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public ModPipeline? Pipeline;
        public float DamageScale;
        public ITargetable? Target;

        private static readonly Random _rng = new();

        public static Vector3 RandomDirectionExcluding(Vector3 parentDir, float excludeHalfAngle = 10f)
        {
            float parentAngle = MathF.Atan2(parentDir.Z, parentDir.X) * (180f / MathF.PI);
            float offset = _rng.NextSingle() * (360f - 2f * excludeHalfAngle) + excludeHalfAngle;
            float angle = (parentAngle + offset) * (MathF.PI / 180f);
            return new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle));
        }
    }
}
