using System;
using Gridlock.Combat;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Mods.Pipeline.Stages
{
    [Serializable]
    public class ConditionalEventStage : IModStage
    {
        public ModPipeline SubPipeline;
        public float DamageScale = 0.6f;
        public ModType ConditionType;

        public StagePhase Phase => StagePhase.OnHit;

        public void Execute(ref ModContext ctx)
        {
            if (SubPipeline == null || ctx.HitObject == null) return;
            if (!EvaluateCondition(ref ctx)) return;
            var target = ctx.HitObject.GetComponent<ITargetable>() ?? ctx.Target;
            ctx.SpawnRequests.Add(new SpawnRequest
            {
                Origin = new Vector3(ctx.Position.x, 0.5f, ctx.Position.z),
                Direction = ctx.Direction,
                Pipeline = SubPipeline.Clone(),
                DamageScale = DamageScale,
                Target = target
            });
        }

        private bool EvaluateCondition(ref ModContext ctx)
        {
            return ConditionType switch
            {
                ModType.IfBurning => ctx.HitObject.GetComponent<StatusEffectManager>()?.HasEffectOfType(StatusEffectType.DamageOverTime) ?? false,
                ModType.IfFrozen => ctx.HitObject.GetComponent<StatusEffectManager>()?.HasEffectOfType(StatusEffectType.Slow) ?? false,
                ModType.IfShocked => ctx.Tags.HasFlag(ModTags.Shock),
                ModType.IfLow => IsLowHP(ctx.HitObject),
                _ => false
            };
        }

        private static bool IsLowHP(GameObject obj)
        {
            var health = obj.GetComponent<EnemyHealth>();
            return health != null && health.CurrentHP / health.MaxHP < 0.3f;
        }

        public IModStage Clone() => new ConditionalEventStage
        {
            SubPipeline = SubPipeline?.Clone(),
            DamageScale = DamageScale,
            ConditionType = ConditionType
        };
    }
}
