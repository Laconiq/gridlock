using System.Numerics;
using Gridlock.Combat;

namespace Gridlock.Mods.Pipeline.Stages
{
    public sealed class ConditionalEventStage : IModStage
    {
        public ModPipeline? SubPipeline;
        public float DamageScale = 0.6f;
        public ModType ConditionType;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null || ctx.HitTarget == null) return;
            if (!EvaluateCondition(ref ctx)) return;
            var target = ctx.HitTarget ?? ctx.Target;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.X, 0.5f, ctx.Position.Z),
                Direction = SpawnRequest.RandomDirectionExcluding(ctx.Direction),
                Pipeline = SubPipeline.Clone(),
                DamageScale = DamageScale,
                Target = target
            });
        }

        private bool EvaluateCondition(ref ModContext ctx)
        {
            return ConditionType switch
            {
                ModType.IfBurning => ctx.HitTarget?.StatusEffects?.HasEffectOfType(StatusEffectType.DamageOverTime) ?? false,
                ModType.IfFrozen => ctx.HitTarget?.StatusEffects?.HasEffectOfType(StatusEffectType.Slow) ?? false,
                ModType.IfLow => IsLowHP(ctx.HitTarget),
                _ => false
            };
        }

        private static bool IsLowHP(ITargetable? target)
        {
            if (target == null || target.MaxHP <= 0f) return false;
            return target.CurrentHP / target.MaxHP < 0.3f;
        }

        public IModStage Clone() => new ConditionalEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale,
            ConditionType = ConditionType
        };
    }
}
