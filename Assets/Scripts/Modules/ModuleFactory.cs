using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Modules.Effects;
using AIWE.Modules.Triggers;
using AIWE.Modules.Zones;
using UnityEngine;

namespace AIWE.Modules
{
    public static class ModuleFactory
    {
        public static List<TriggerInstance> CreateTriggers(TriggerDefinition def, IChassis owner)
        {
            var instances = new List<TriggerInstance>();
            foreach (var template in def.triggers)
            {
                if (template == null) continue;
                var instance = template.CreateInstance();
                instance.Definition = def;
                instance.Owner = owner;
                instances.Add(instance);
            }
            return instances;
        }

        public static List<ZoneInstance> CreateZones(ZoneDefinition def, IChassis owner)
        {
            var instances = new List<ZoneInstance>();
            foreach (var template in def.zones)
            {
                if (template == null) continue;
                var instance = template.CreateInstance();
                instance.Definition = def;
                instance.Owner = owner;
                instances.Add(instance);
            }
            return instances;
        }

        public static List<EffectInstance> CreateEffects(EffectDefinition def, IChassis owner)
        {
            var instances = new List<EffectInstance>();
            foreach (var template in def.effects)
            {
                if (template == null) continue;
                var instance = template.CreateInstance();
                instance.Definition = def;
                instance.Owner = owner;
                instances.Add(instance);
            }
            return instances;
        }
    }
}
