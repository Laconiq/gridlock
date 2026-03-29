using Gridlock.UI;
using UnityEngine;

namespace Gridlock.Mods.UI
{
    public static class ModSlotColors
    {
        public static readonly Color Burn = new(1f, 0.5f, 0.1f);
        public static readonly Color Frost = new(0.4f, 0.8f, 1f);
        public static readonly Color Shock = new(1f, 0.95f, 0.3f);
        public static readonly Color Void = new(0.7f, 0.2f, 0.9f);
        public static readonly Color Leech = new(0.2f, 0.9f, 0.4f);

        public static Color GetModColor(ModType type)
        {
            if (type.IsEvent())
                return DesignConstants.Tertiary;

            return type switch
            {
                ModType.Burn => Burn,
                ModType.Frost => Frost,
                ModType.Shock => Shock,
                ModType.Void => Void,
                ModType.Leech => Leech,
                _ => DesignConstants.Primary
            };
        }

        public static string GetModDisplayName(ModType type)
        {
            if (type.IsEvent())
            {
                string name = type.ToString().ToUpperInvariant();
                if (name.StartsWith("ON"))
                    name = name.Substring(2);
                else if (name.StartsWith("IF"))
                    name = name.Substring(2);
                return "\u27D0 " + name;
            }

            return type.ToString().ToUpperInvariant();
        }

        public static string GetModDescription(ModType type)
        {
            return type switch
            {
                ModType.Homing => "Projectile tracks the target",
                ModType.Pierce => "Passes through enemies (max 3)",
                ModType.Bounce => "Bounces between enemies (max 3)",
                ModType.Split => "Splits into 3 sub-projectiles",
                ModType.Heavy => "x2 DMG, x1.5 Size, x0.6 Speed",
                ModType.Swift => "x1.8 Speed, x0.8 Fire interval, x0.6 DMG",
                ModType.Wide => "Impact deals AOE damage (radius 2)",
                ModType.Burn => "Fire DOT: 5 dmg/s for 3s",
                ModType.Frost => "Slow: -50% speed for 2s",
                ModType.Shock => "Chains to 1 nearby enemy",
                ModType.Void => "Deals 8% of target's current HP",
                ModType.Leech => "Heals objective for 20% of damage",
                ModType.OnHit => "Triggers on every impact",
                ModType.OnKill => "Triggers when projectile kills",
                ModType.OnEnd => "Triggers when projectile expires",
                ModType.OnPierce => "Triggers each time bullet pierces (requires PIERCE)",
                ModType.OnBounce => "Triggers each time bullet bounces (requires BOUNCE)",
                ModType.OnChain => "Triggers when shock chains (requires SHOCK)",
                ModType.OnDelay => "Triggers after 0.5s of flight",
                ModType.OnPulse => "Triggers every 0.3s while alive",
                ModType.IfBurning => "Triggers if target is on fire",
                ModType.IfFrozen => "Triggers if target is slowed/frozen",
                ModType.IfShocked => "Triggers if target has shock debuff",
                ModType.IfLow => "Triggers if target is below 30% HP",
                ModType.OnCrit => "Triggers on critical hit (10% chance)",
                ModType.OnOverkill => "Triggers if damage exceeds remaining HP",
                _ => ""
            };
        }

        public static string GetModCategory(ModType type)
        {
            if (type.IsConditional()) return "CONDITIONAL";
            if (type.IsModDependent()) return "MOD_DEPENDENT";
            if (type.IsEvent()) return "EVENT";
            if (type.IsElemental()) return "ELEMENT";
            return "BEHAVIOR";
        }

        public static string GetSynergyDescription(SynergyEffect effect)
        {
            return effect switch
            {
                SynergyEffect.Railgun => "HEAVY + HEAVY\nGrants free Pierce with +2 pierce count",
                SynergyEffect.Machinegun => "SWIFT + SWIFT\nDoubles the tower fire rate",
                SynergyEffect.Blizzard => "FROST + FROST\nSlow becomes a 0.5s freeze stun",
                SynergyEffect.Tesla => "SHOCK + SHOCK\nChain lightning hits 3 enemies instead of 1",
                SynergyEffect.Napalm => "BURN + WIDE\nCreates a persistent fire zone on ground (3s)",
                SynergyEffect.Avalanche => "FROST + WIDE\nCreates a persistent slow zone on ground (3s)",
                SynergyEffect.Ricochet => "PIERCE + BOUNCE\nUnlimited pierce and bounces for 2s",
                SynergyEffect.Missile => "HOMING + SWIFT\nPerfect homing — instant snap, no curve",
                SynergyEffect.Meteor => "HEAVY + WIDE\nAOE radius doubled, screen shake on impact",
                SynergyEffect.ThermalShock => "BURN + FROST\nBurst damage = 50% max HP (consumes both statuses)",
                SynergyEffect.Siphon => "SHOCK + VOID\nVoid damage heals the objective",
                SynergyEffect.Barrage => "SPLIT + SPLIT\nSplits into 5 instead of 3+3",
                SynergyEffect.Vampire => "LEECH + HEAVY\nDrain increased to 40% of damage dealt",
                _ => ""
            };
        }
    }
}
