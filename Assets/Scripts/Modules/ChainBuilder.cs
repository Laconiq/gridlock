using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Modules.Effects;
using AIWE.Modules.Triggers;
using AIWE.Modules.Zones;
using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Modules
{
    public class TriggerChain
    {
        public TriggerInstance Trigger;
        public readonly List<ZoneChain> ZoneChains = new();
    }

    public class ZoneChain
    {
        public ZoneInstance Zone;
        public readonly List<EffectInstance> Effects = new();
        public readonly List<ZoneChain> ChainedZones = new();
    }

    public static class ChainBuilder
    {
        public const int MaxChainDepth = 8;

        public static void BuildZoneChains(
            NodeGraphData graph,
            string fromNodeId,
            List<ZoneChain> zoneChains,
            ModuleRegistry registry,
            IChassis chassis,
            int depth = 0)
        {
            if (depth >= MaxChainDepth) return;

            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;

                var targetNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (targetNode == null) continue;

                if (targetNode.category == ModuleCategory.Zone)
                {
                    var zoneDef = registry.GetById(targetNode.moduleDefId) as ZoneDefinition;
                    if (zoneDef == null) continue;

                    var zones = ModuleFactory.CreateZones(zoneDef, chassis);
                    if (zones.Count == 0) continue;

                    var zoneChain = new ZoneChain { Zone = zones[0] };
                    CollectEffects(graph, targetNode.nodeId, zoneChain.Effects, registry, chassis);
                    BuildZoneChains(graph, targetNode.nodeId, zoneChain.ChainedZones, registry, chassis, depth + 1);
                    zoneChains.Add(zoneChain);
                }
            }
        }

        public static void CollectEffects(
            NodeGraphData graph,
            string fromNodeId,
            List<EffectInstance> effects,
            ModuleRegistry registry,
            IChassis chassis,
            int depth = 0)
        {
            if (depth >= MaxChainDepth) return;

            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;
                var effNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (effNode?.category != ModuleCategory.Effect) continue;

                var effDef = registry.GetById(effNode.moduleDefId) as EffectDefinition;
                if (effDef == null) continue;

                var effInstances = ModuleFactory.CreateEffects(effDef, chassis);
                effects.AddRange(effInstances);
                CollectEffects(graph, effNode.nodeId, effects, registry, chassis, depth + 1);
            }
        }

        public static void ExecuteZoneChain(ZoneChain zoneChain, Vector3 origin, float range)
        {
            if (!zoneChain.Zone.IsReady) return;

            var targets = zoneChain.Zone.SelectTargets(origin, range);
            zoneChain.Zone.StartCooldown();

            foreach (var effect in zoneChain.Effects)
            {
                if (!effect.IsReady) continue;
                effect.Execute(targets, origin);
                effect.StartCooldown();
            }

            foreach (var chained in zoneChain.ChainedZones)
            {
                ExecuteZoneChain(chained, origin, range);
            }
        }

        public static void TickCooldowns(List<ZoneChain> zoneChains, float dt)
        {
            foreach (var zc in zoneChains)
            {
                zc.Zone.TickCooldown(dt);
                foreach (var eff in zc.Effects)
                    eff.TickCooldown(dt);
                TickCooldowns(zc.ChainedZones, dt);
            }
        }
    }
}
