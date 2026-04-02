using System;
using System.Numerics;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class SplitStage : IModStage
    {
        public float ArcDegrees { get; set; } = 90f;
        public int ExtraCount { get; set; } = 1;
        public int BarrageBonus { get; set; }

        public StagePhase Phase => StagePhase.Configure;

        public void Execute(ref ModContext ctx)
        {
            int extras = ExtraCount + BarrageBonus;
            if (extras <= 0) return;

            int total = 1 + extras;
            var baseDir = ctx.Direction;
            if (baseDir.LengthSquared() < 0.001f) baseDir = Vector3.UnitZ;

            float startAngle = -ArcDegrees * 0.5f;
            float step = total > 1 ? ArcDegrees / (total - 1) : 0f;

            ctx.Direction = RotateAroundY(baseDir, startAngle);

            for (int i = 1; i < total; i++)
            {
                float angle = startAngle + step * i;
                var dir = RotateAroundY(baseDir, angle);
                ctx.SpawnRequests.Add(new SpawnRequest
                {
                    Origin = ctx.Position,
                    Direction = dir,
                    Pipeline = ctx.OwnerPipeline?.CloneExcludingPhase(StagePhase.Configure),
                    DamageScale = 1f,
                    Target = ctx.Target,
                });
            }
        }

        private static Vector3 RotateAroundY(Vector3 dir, float degrees)
        {
            float rad = degrees * (MathF.PI / 180f);
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);
            return new Vector3(
                dir.X * cos + dir.Z * sin,
                dir.Y,
                -dir.X * sin + dir.Z * cos
            );
        }

        public IModStage Clone() => new SplitStage
        {
            ArcDegrees = ArcDegrees,
            ExtraCount = ExtraCount,
            BarrageBonus = BarrageBonus
        };
    }
}
