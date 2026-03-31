using System.Collections.Generic;
using Gridlock.Mods.Pipeline.Stages;

namespace Gridlock.Mods.Pipeline
{
    public static class PipelineCompiler
    {
        public static (ModPipeline pipeline, ModContext baseContext) Compile(
            List<ModSlotData> slots, float baseDamage, List<SynergyEffect> activeSynergies)
        {
            DetectSynergies(slots, activeSynergies);

            var groups = SplitAtEvents(slots);
            var pipeline = BuildNestedPipeline(groups, activeSynergies);

            var ctx = ModContext.Create(baseDamage, speed: 10f, size: 0.2f, lifetime: 5f);
            ctx.Synergies = new List<SynergyEffect>(activeSynergies);
            ctx.Tags = pipeline.AccumulatedTags;

            ApplyContextSynergies(ref ctx, pipeline, activeSynergies);

            return (pipeline, ctx);
        }

        private static void DetectSynergies(List<ModSlotData> slots, List<SynergyEffect> synergies)
        {
            synergies.Clear();
            int groupStart = 0;
            for (int s = 0; s <= slots.Count; s++)
            {
                if (s < slots.Count && !slots[s].modType.IsEvent()) continue;

                for (int i = groupStart; i < s - 1; i++)
                {
                    for (int j = i + 1; j < s; j++)
                    {
                        var syn = SynergyTable.Check(slots[i].modType, slots[j].modType);
                        if (syn.HasValue && !synergies.Contains(syn.Value.effect))
                            synergies.Add(syn.Value.effect);
                    }
                }

                groupStart = s + 1;
            }
        }

        private struct TraitGroup
        {
            public List<ModType> traits;
            public ModType? eventType;
        }

        // Each group = { traits that follow this event, which event preceded them }
        // Group 0 always has eventType=null (traits before any event = main projectile)
        private static List<TraitGroup> SplitAtEvents(List<ModSlotData> slots)
        {
            var groups = new List<TraitGroup>();
            var currentTraits = new List<ModType>();
            ModType? pendingEvent = null;

            foreach (var slot in slots)
            {
                if (slot.modType.IsTrait())
                {
                    currentTraits.Add(slot.modType);
                    continue;
                }

                groups.Add(new TraitGroup { traits = currentTraits, eventType = pendingEvent });
                currentTraits = new List<ModType>();
                pendingEvent = slot.modType;
            }

            groups.Add(new TraitGroup { traits = currentTraits, eventType = pendingEvent });
            return groups;
        }

        // Build right-to-left so events are properly nested:
        // [A, ⟐Hit, B, ⟐Kill, C] → Main:[A, OnHit(sub:[B, OnKill(sub:[C])])]
        private static ModPipeline BuildNestedPipeline(List<TraitGroup> groups, List<SynergyEffect> synergies)
        {
            ModPipeline currentSub = null;
            ModType? pendingEvent = null;

            for (int i = groups.Count - 1; i >= 0; i--)
            {
                var pipeline = new ModPipeline();
                if (groups[i].traits.Count > 0)
                    AddTraitStages(groups[i].traits, pipeline, synergies);

                if (pendingEvent.HasValue)
                    AddEventStage(pendingEvent.Value, currentSub, pipeline, synergies);

                currentSub = pipeline;
                pendingEvent = groups[i].eventType;
            }

            return currentSub ?? new ModPipeline();
        }

        private static int CountTrait(List<ModType> traits, ModType type)
        {
            int c = 0;
            for (int i = 0; i < traits.Count; i++)
                if (traits[i] == type) c++;
            return c;
        }

        private static int CountAdjacentPairs(List<ModType> traits, ModType type)
        {
            int pairs = 0;
            for (int i = 1; i < traits.Count; i++)
                if (traits[i] == type && traits[i - 1] == type) pairs++;
            return pairs;
        }

        private static void AddTraitStages(List<ModType> traits, ModPipeline pipeline, List<SynergyEffect> synergies)
        {
            var set = new HashSet<ModType>(traits);

            int heavyTotal = CountTrait(traits, ModType.Heavy) + CountAdjacentPairs(traits, ModType.Heavy);
            for (int i = 0; i < heavyTotal; i++)
                pipeline.AddStage(new HeavyStage(), ModTags.Heavy);

            int swiftTotal = CountTrait(traits, ModType.Swift) + CountAdjacentPairs(traits, ModType.Swift);
            for (int i = 0; i < swiftTotal; i++)
                pipeline.AddStage(new SwiftStage(), ModTags.Swift);

            int splitCount = CountTrait(traits, ModType.Split);
            if (splitCount > 0)
            {
                int barrageBonus = CountAdjacentPairs(traits, ModType.Split);
                pipeline.AddStage(new SplitStage { ExtraCount = splitCount, BarrageBonus = barrageBonus }, ModTags.Split);
            }
            if (set.Contains(ModType.Homing))
                pipeline.AddStage(new HomingStage(), ModTags.Homing);

            int voidTotal = CountTrait(traits, ModType.Void) + CountAdjacentPairs(traits, ModType.Void);
            for (int i = 0; i < voidTotal; i++)
                pipeline.AddStage(new VoidStage(), ModTags.Void);

            int wideTotal = CountTrait(traits, ModType.Wide) + CountAdjacentPairs(traits, ModType.Wide);
            for (int i = 0; i < wideTotal; i++)
                pipeline.AddStage(new WideStage(), ModTags.Wide);

            int burnTotal = CountTrait(traits, ModType.Burn) + CountAdjacentPairs(traits, ModType.Burn);
            for (int i = 0; i < burnTotal; i++)
                pipeline.AddStage(new BurnStage(), ModTags.Burn);

            int frostTotal = CountTrait(traits, ModType.Frost) + CountAdjacentPairs(traits, ModType.Frost);
            for (int i = 0; i < frostTotal; i++)
                pipeline.AddStage(new FrostStage(), ModTags.Frost);

            int shockTotal = CountTrait(traits, ModType.Shock) + CountAdjacentPairs(traits, ModType.Shock);
            for (int i = 0; i < shockTotal; i++)
                pipeline.AddStage(new ShockStage(), ModTags.Shock);

            int leechTotal = CountTrait(traits, ModType.Leech) + CountAdjacentPairs(traits, ModType.Leech);
            for (int i = 0; i < leechTotal; i++)
                pipeline.AddStage(new LeechStage(), ModTags.Leech);

            pipeline.AddStage(new ImpactFeedbackStage(), ModTags.None);

            bool hasPierce = set.Contains(ModType.Pierce);
            if (!hasPierce && synergies.Contains(SynergyEffect.Railgun))
                hasPierce = true;
            if (hasPierce)
                pipeline.AddStage(new PierceStage(), ModTags.Pierce);

            if (set.Contains(ModType.Bounce))
                pipeline.AddStage(new BounceStage(), ModTags.Bounce);
        }

        private static void AddEventStage(ModType eventType, ModPipeline subPipeline, ModPipeline targetPipeline, List<SynergyEffect> synergies)
        {
            float scale = EventDamageScale(eventType);

            IModStage stage = eventType switch
            {
                ModType.OnHit => new OnHitEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnKill => new OnKillEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnEnd => new OnEndEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnPulse => new OnPulseEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnDelay => new OnDelayEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnOverkill =>
                    new OnOverkillEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.IfBurning or ModType.IfFrozen or ModType.IfLow =>
                    new ConditionalEventStage { SubPipeline = subPipeline, DamageScale = scale, ConditionType = eventType },
                _ => null
            };

            if (stage != null)
                targetPipeline.AddStage(stage, ModTags.None);
        }

        private static float EventDamageScale(ModType eventType)
        {
            return eventType switch
            {
                ModType.OnPulse => 0.3f,
                ModType.OnOverkill => 1.0f,
                _ => 0.6f
            };
        }

        private static void ApplyContextSynergies(ref ModContext ctx, ModPipeline pipeline, List<SynergyEffect> synergies)
        {
            int pierceCount = pipeline.AccumulatedTags.HasFlag(ModTags.Pierce) ? 3 : 0;
            int bounceCount = pipeline.AccumulatedTags.HasFlag(ModTags.Bounce) ? 3 : 0;

            if (synergies.Contains(SynergyEffect.Railgun))
                pierceCount += 2;

            ctx.PierceRemaining = pierceCount;
            ctx.BounceRemaining = bounceCount;
        }
    }
}
