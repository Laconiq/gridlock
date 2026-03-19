using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Modules.Effects;
using AIWE.Modules.Triggers;
using AIWE.Modules.Zones;
using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Modules
{
    public static class ModuleFactory
    {
        private static GameObject _projectilePrefab;

        public static void SetProjectilePrefab(GameObject prefab)
        {
            _projectilePrefab = prefab;
        }

        public static TriggerInstance CreateTrigger(TriggerDefinition def, IChassis owner, List<ParamOverride> overrides = null)
        {
            var cooldown = def.defaultCooldown;
            var range = 10f;

            if (overrides != null)
            {
                foreach (var p in overrides)
                {
                    if (p.paramName == "cooldown") cooldown = p.value;
                    if (p.paramName == "range") range = p.value;
                }
            }

            TriggerInstance instance = def.moduleId switch
            {
                "on_enemy_enter_range" => new OnEnemyEnterRangeTrigger(range, cooldown),
                "on_timer" => new OnTimerTrigger(cooldown),
                _ => throw new ArgumentException($"Unknown trigger: {def.moduleId}")
            };

            instance.Definition = def;
            instance.Owner = owner;
            return instance;
        }

        public static ZoneInstance CreateZone(ZoneDefinition def, IChassis owner, List<ParamOverride> overrides = null)
        {
            ZoneInstance instance = def.moduleId switch
            {
                "nearest_enemy" => new NearestEnemyZone(),
                "all_enemies_in_range" => new AllEnemiesInRangeZone(),
                _ => throw new ArgumentException($"Unknown zone: {def.moduleId}")
            };

            instance.Definition = def;
            instance.Owner = owner;
            return instance;
        }

        public static EffectInstance CreateEffect(EffectDefinition def, IChassis owner, List<ParamOverride> overrides = null)
        {
            var damage = def.defaultDamage;
            var duration = def.defaultDuration;

            if (overrides != null)
            {
                foreach (var p in overrides)
                {
                    if (p.paramName == "damage") damage = p.value;
                    if (p.paramName == "duration") duration = p.value;
                }
            }

            EffectInstance instance = def.moduleId switch
            {
                "projectile" => new ProjectileEffect(damage, 20f, _projectilePrefab),
                "hitscan" => new HitscanEffect(damage),
                "slow" => new SlowEffect(0.5f, duration),
                "dot" => new DotEffect(damage, duration),
                _ => throw new ArgumentException($"Unknown effect: {def.moduleId}")
            };

            instance.Definition = def;
            instance.Owner = owner;
            return instance;
        }
    }
}
