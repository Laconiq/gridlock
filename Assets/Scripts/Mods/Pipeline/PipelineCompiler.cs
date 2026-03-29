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

            var traitGroups = SplitAtEvents(slots);
            var pipeline = new ModPipeline();

            if (traitGroups.Count > 0)
                AddTraitStages(traitGroups[0].traits, pipeline, activeSynergies);

            for (int i = 0; i < traitGroups.Count; i++)
            {
                if (traitGroups[i].eventType.HasValue)
                    AddEventStage(traitGroups[i], pipeline, activeSynergies);
            }

            var ctx = ModContext.Create(baseDamage, speed: 20f, size: 0.2f, lifetime: 5f);
            ctx.Synergies = new List<SynergyEffect>(activeSynergies);
            ctx.Tags = pipeline.AccumulatedTags;

            ApplyContextSynergies(ref ctx, pipeline, activeSynergies);

            return (pipeline, ctx);
        }

        private static void DetectSynergies(List<ModSlotData> slots, List<SynergyEffect> synergies)
        {
            synergies.Clear();
            for (int i = 0; i < slots.Count - 1; i++)
            {
                if (slots[i].modType.IsEvent()) continue;
                for (int j = i + 1; j < slots.Count; j++)
                {
                    if (slots[j].modType.IsEvent()) continue;
                    var syn = SynergyTable.Check(slots[i].modType, slots[j].modType);
                    if (syn.HasValue && !synergies.Contains(syn.Value.effect))
                        synergies.Add(syn.Value.effect);
                }
            }
        }

        private struct TraitGroup
        {
            public List<ModType> traits;
            public ModType? eventType;
        }

        private static List<TraitGroup> SplitAtEvents(List<ModSlotData> slots)
        {
            var groups = new List<TraitGroup>();
            var currentTraits = new List<ModType>();
            var traitsSoFar = new HashSet<ModType>();

            foreach (var slot in slots)
            {
                if (slot.modType.IsTrait())
                {
                    currentTraits.Add(slot.modType);
                    traitsSoFar.Add(slot.modType);
                    continue;
                }

                var required = slot.modType.RequiredMod();
                if (required.HasValue && !traitsSoFar.Contains(required.Value))
                    continue;

                groups.Add(new TraitGroup { traits = currentTraits, eventType = null });
                groups.Add(new TraitGroup { traits = new List<ModType>(), eventType = slot.modType });
                currentTraits = new List<ModType>();
            }

            if (groups.Count == 0)
            {
                groups.Add(new TraitGroup { traits = currentTraits, eventType = null });
            }
            else if (currentTraits.Count > 0)
            {
                int lastEventIdx = -1;
                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    if (groups[i].eventType.HasValue) { lastEventIdx = i; break; }
                }
                if (lastEventIdx >= 0)
                {
                    var g = groups[lastEventIdx];
                    g.traits = currentTraits;
                    groups[lastEventIdx] = g;
                }
            }

            return groups;
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

            // Stackable traits — count + adjacent pair bonus
            int heavyTotal = CountTrait(traits, ModType.Heavy) + CountAdjacentPairs(traits, ModType.Heavy);
            for (int i = 0; i < heavyTotal; i++)
                pipeline.AddStage(new HeavyStage(), ModTags.Heavy);

            int swiftTotal = CountTrait(traits, ModType.Swift) + CountAdjacentPairs(traits, ModType.Swift);
            for (int i = 0; i < swiftTotal; i++)
                pipeline.AddStage(new SwiftStage(), ModTags.Swift);

            // Split — 1x stage, count + adjacent pair bonus determines projectiles
            int splitCount = CountTrait(traits, ModType.Split);
            if (splitCount > 0)
            {
                int barrageBonus = CountAdjacentPairs(traits, ModType.Split);
                pipeline.AddStage(new SplitStage { ExtraCount = splitCount, BarrageBonus = barrageBonus }, ModTags.Split);
            }
            if (set.Contains(ModType.Homing))
                pipeline.AddStage(new HomingStage(), ModTags.Homing);

            // Stackable elemental/effect traits — count + adjacent pair bonus
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

            // Structural — 1x, count handled via PierceRemaining/BounceRemaining
            bool hasPierce = set.Contains(ModType.Pierce);
            if (!hasPierce && synergies.Contains(SynergyEffect.Railgun))
                hasPierce = true;
            if (hasPierce)
                pipeline.AddStage(new PierceStage(), ModTags.Pierce);

            if (set.Contains(ModType.Bounce))
                pipeline.AddStage(new BounceStage(), ModTags.Bounce);
        }

        private static void AddEventStage(TraitGroup group, ModPipeline pipeline, List<SynergyEffect> synergies)
        {
            if (!group.eventType.HasValue) return;

            ModPipeline subPipeline = null;
            if (group.traits.Count > 0)
            {
                subPipeline = new ModPipeline();
                AddTraitStages(group.traits, subPipeline, synergies);
            }

            var eventType = group.eventType.Value;
            float scale = EventDamageScale(eventType);

            IModStage stage = eventType switch
            {
                ModType.OnHit => new OnHitEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnKill => new OnKillEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnEnd => new OnEndEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnPierce => new OnPierceEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnBounce => new OnBounceEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnPulse => new OnPulseEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnDelay => new OnDelayEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.OnCrit or ModType.OnOverkill =>
                    new OnOverkillEventStage { SubPipeline = subPipeline, DamageScale = scale },
                ModType.IfBurning or ModType.IfFrozen or ModType.IfShocked or ModType.IfLow =>
                    new ConditionalEventStage { SubPipeline = subPipeline, DamageScale = scale, ConditionType = eventType },
                _ => null
            };

            if (stage != null)
                pipeline.AddStage(stage, ModTags.None);
        }

        private static float EventDamageScale(ModType eventType)
        {
            return eventType switch
            {
                ModType.OnPulse => 0.3f,
                ModType.OnCrit => 0.8f,
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
