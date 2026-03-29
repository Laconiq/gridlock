using System.Collections.Generic;
using Gridlock.Interfaces;
using Gridlock.Modules.Effects;
using Gridlock.Modules.Triggers;
using Gridlock.Modules.Zones;
using Gridlock.NodeEditor.Data;
using UnityEngine;

namespace Gridlock.Modules
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
            int depth = 0,
            HashSet<string> visited = null)
        {
            if (depth >= MaxChainDepth) return;
            visited ??= new HashSet<string>();

            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;

                var targetNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (targetNode == null) continue;
                if (!visited.Add(targetNode.nodeId)) continue;

                if (targetNode.category == ModuleCategory.Zone)
                {
                    var zoneDef = registry.GetById(targetNode.moduleDefId) as ZoneDefinition;
                    if (zoneDef == null) continue;

                    var zones = ModuleFactory.CreateZones(zoneDef, chassis);
                    if (zones.Count == 0) continue;

                    var zoneChain = new ZoneChain { Zone = zones[0] };
                    CollectEffects(graph, targetNode.nodeId, zoneChain.Effects, registry, chassis, 0, visited);
                    BuildZoneChains(graph, targetNode.nodeId, zoneChain.ChainedZones, registry, chassis, depth + 1, visited);
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
            int depth = 0,
            HashSet<string> visited = null)
        {
            if (depth >= MaxChainDepth) return;
            visited ??= new HashSet<string>();

            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;
                var effNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (effNode?.category != ModuleCategory.Effect) continue;
                if (!visited.Add(effNode.nodeId)) continue;

                var effDef = registry.GetById(effNode.moduleDefId) as EffectDefinition;
                if (effDef == null) continue;

                var effInstances = ModuleFactory.CreateEffects(effDef, chassis);
                effects.AddRange(effInstances);
                CollectEffects(graph, effNode.nodeId, effects, registry, chassis, depth + 1, visited);
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
