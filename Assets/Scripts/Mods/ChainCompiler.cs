using System.Collections.Generic;

namespace Gridlock.Mods
{
    public static class ChainCompiler
    {
        private static class EventDamageScale
        {
            public static float For(ModType eventType)
            {
                return eventType switch
                {
                    ModType.OnPulse => 0.3f,
                    ModType.OnCrit => 0.8f,
                    ModType.OnOverkill => 1f,
                    _ => 0.6f
                };
            }
        }

        public static ProjectileConfig Compile(List<ModSlotData> slots, float baseDamage, List<SynergyEffect> activeSynergies)
        {
            activeSynergies.Clear();

            // Detect adjacency synergies (events break adjacency)
            for (int i = 0; i < slots.Count - 1; i++)
            {
                if (slots[i].modType.IsEvent() || slots[i + 1].modType.IsEvent()) continue;
                var syn = SynergyTable.Check(slots[i].modType, slots[i + 1].modType);
                if (syn.HasValue && !activeSynergies.Contains(syn.Value.effect))
                    activeSynergies.Add(syn.Value.effect);
            }

            // Split chain into stages at events
            var stages = new List<List<ModType>>();
            var events = new List<ModType>();
            var current = new List<ModType>();

            // Accumulates all trait mods across all stages for mod-dependent event validation
            var traitsSoFar = new HashSet<ModType>();

            foreach (var slot in slots)
            {
                if (slot.modType.IsEvent())
                {
                    var required = slot.modType.RequiredMod();
                    if (required.HasValue && !traitsSoFar.Contains(required.Value))
                        continue;

                    stages.Add(current);
                    events.Add(slot.modType);
                    current = new List<ModType>();
                }
                else
                {
                    current.Add(slot.modType);
                    traitsSoFar.Add(slot.modType);
                }
            }
            stages.Add(current);

            // Build main projectile from stage 0
            var config = BuildFromTraits(stages[0], baseDamage, activeSynergies);

            // Build sub-projectiles from subsequent stages with per-event damage scaling
            config.eventStages = new List<EventStage>();
            for (int i = 0; i < events.Count; i++)
            {
                var subTraits = i + 1 < stages.Count ? stages[i + 1] : new List<ModType>();
                float subDamage = baseDamage * EventDamageScale.For(events[i]);
                var subConfig = BuildFromTraits(subTraits, subDamage, activeSynergies);

                config.eventStages.Add(new EventStage
                {
                    eventType = events[i],
                    subProjectile = subConfig
                });
            }

            return config;
        }

        private static ProjectileConfig BuildFromTraits(List<ModType> traits, float baseDamage, List<SynergyEffect> synergies)
        {
            var config = ProjectileConfig.Default(baseDamage);

            float damageMult = 1f;
            float speedMult = 1f;
            float sizeMult = 1f;

            foreach (var trait in traits)
            {
                switch (trait)
                {
                    case ModType.Homing: config.homing = true; break;
                    case ModType.Pierce: config.pierce = true; break;
                    case ModType.Bounce: config.bounce = true; break;
                    case ModType.Split: config.split = true; break;
                    case ModType.Heavy: damageMult *= 2f; speedMult *= 0.6f; sizeMult *= 1.5f; break;
                    case ModType.Swift: damageMult *= 0.6f; speedMult *= 1.8f; break;
                    case ModType.Wide: config.wide = true; break;
                    case ModType.Burn: config.burn = true; break;
                    case ModType.Frost: config.frost = true; break;
                    case ModType.Shock: config.shock = true; break;
                    case ModType.Void: config.isVoid = true; break;
                    case ModType.Leech: config.leech = true; break;
                }
            }

            foreach (var syn in synergies)
            {
                switch (syn)
                {
                    case SynergyEffect.Railgun:
                        config.pierce = true;
                        config.pierceCount += 2;
                        break;
                    case SynergyEffect.Machinegun:
                        break; // Handled at tower level (fire rate)
                    case SynergyEffect.Blizzard:
                        break; // Handled at frost application (stun flag)
                    case SynergyEffect.Tesla:
                        config.bounceCount = 3;
                        break;
                    case SynergyEffect.Napalm:
                        config.napalm = true;
                        break;
                    case SynergyEffect.Avalanche:
                        config.avalanche = true;
                        break;
                    case SynergyEffect.Ricochet:
                        config.ricochet = true;
                        break;
                    case SynergyEffect.Missile:
                        break; // Handled at homing (perfect tracking)
                    case SynergyEffect.Meteor:
                        config.wideRadius *= 2f;
                        break;
                    case SynergyEffect.ThermalShock:
                        config.thermalShock = true;
                        break;
                    case SynergyEffect.Siphon:
                        config.siphon = true;
                        break;
                    case SynergyEffect.Barrage:
                        config.splitCount = 5;
                        break;
                    case SynergyEffect.Vampire:
                        break; // Handled at leech application (40%)
                }
            }

            config.damage = baseDamage * damageMult;
            config.speed *= speedMult;
            config.size *= sizeMult;

            return config;
        }
    }
}
