using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Modules;
using AIWE.Modules.Effects;
using AIWE.Modules.Triggers;
using AIWE.Modules.Zones;
using AIWE.NodeEditor.Data;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Towers
{
    public class TowerExecutor : NetworkBehaviour
    {
        [SerializeField] private ModuleRegistry moduleRegistry;
        [SerializeField] private GameObject projectilePrefab;

        private IChassis _chassis;
        private readonly List<TriggerChain> _triggerChains = new();
        private bool _initialized;

        public override void OnNetworkSpawn()
        {
            _chassis = GetComponent<IChassis>();

            if (projectilePrefab != null)
                ModuleFactory.SetProjectilePrefab(projectilePrefab);

            var towerChassis = GetComponent<TowerChassis>();
            if (towerChassis != null)
            {
                RebuildFromGraph(towerChassis.GetNodeGraph());
            }
        }

        public void RebuildFromGraph(NodeGraphData graph)
        {
            _triggerChains.Clear();
            _initialized = false;

            if (graph == null || graph.nodes.Count == 0 || moduleRegistry == null || _chassis == null)
                return;

            // Find all trigger nodes
            foreach (var node in graph.nodes)
            {
                if (node.category != ModuleCategory.Trigger) continue;

                var triggerDef = moduleRegistry.GetById(node.moduleDefId) as TriggerDefinition;
                if (triggerDef == null) continue;

                var triggerInstance = ModuleFactory.CreateTrigger(triggerDef, _chassis, node.paramOverrides);

                var chain = new TriggerChain { Trigger = triggerInstance };

                // Follow connections from this trigger
                BuildZoneChain(graph, node.nodeId, chain.ZoneChains);

                // Wire up trigger event
                chain.Trigger.OnTriggered += () => ExecuteChain(chain);

                _triggerChains.Add(chain);
            }

            _initialized = _triggerChains.Count > 0;
            Debug.Log($"[TowerExecutor] Built {_triggerChains.Count} trigger chains on {gameObject.name}");
        }

        private void BuildZoneChain(NodeGraphData graph, string fromNodeId, List<ZoneChain> zoneChains)
        {
            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;

                var targetNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (targetNode == null) continue;

                if (targetNode.category == ModuleCategory.Zone)
                {
                    var zoneDef = moduleRegistry.GetById(targetNode.moduleDefId) as ZoneDefinition;
                    if (zoneDef == null) continue;

                    var zoneInstance = ModuleFactory.CreateZone(zoneDef, _chassis, targetNode.paramOverrides);
                    var zoneChain = new ZoneChain { Zone = zoneInstance };

                    // Find effects connected to this zone
                    foreach (var effConn in graph.connections)
                    {
                        if (effConn.fromNodeId != targetNode.nodeId) continue;
                        var effNode = graph.nodes.Find(n => n.nodeId == effConn.toNodeId);
                        if (effNode?.category != ModuleCategory.Effect) continue;

                        var effDef = moduleRegistry.GetById(effNode.moduleDefId) as EffectDefinition;
                        if (effDef == null) continue;

                        var effInstance = ModuleFactory.CreateEffect(effDef, _chassis, effNode.paramOverrides);
                        zoneChain.Effects.Add(effInstance);
                    }

                    // Recurse for chained zones
                    BuildZoneChain(graph, targetNode.nodeId, zoneChain.ChainedZones);

                    zoneChains.Add(zoneChain);
                }
            }
        }

        private void Update()
        {
            if (!IsServer || !_initialized) return;

            foreach (var chain in _triggerChains)
            {
                chain.Trigger.Tick(Time.deltaTime);
            }
        }

        private void ExecuteChain(TriggerChain chain)
        {
            if (_chassis?.FirePoint == null) return;

            var origin = _chassis.FirePoint.position;
            var range = 10f;

            if (_chassis is TowerChassis tc && tc.Definition != null)
                range = tc.Definition.baseRange;

            foreach (var zoneChain in chain.ZoneChains)
            {
                ExecuteZoneChain(zoneChain, origin, range);
            }
        }

        private void ExecuteZoneChain(ZoneChain zoneChain, Vector3 origin, float range)
        {
            var targets = zoneChain.Zone.SelectTargets(origin, range);

            foreach (var effect in zoneChain.Effects)
            {
                effect.Execute(targets, origin);
            }

            foreach (var chained in zoneChain.ChainedZones)
            {
                ExecuteZoneChain(chained, origin, range);
            }
        }

        private class TriggerChain
        {
            public TriggerInstance Trigger;
            public readonly List<ZoneChain> ZoneChains = new();
        }

        private class ZoneChain
        {
            public ZoneInstance Zone;
            public readonly List<EffectInstance> Effects = new();
            public readonly List<ZoneChain> ChainedZones = new();
        }
    }
}
