using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Modules;
using AIWE.Modules.Effects;
using AIWE.Modules.Triggers;
using AIWE.Modules.Zones;
using AIWE.NodeEditor.Data;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerWeaponExecutor : NetworkBehaviour
    {
        [SerializeField] private ModuleRegistry moduleRegistry;

        private PlayerWeaponChassis _chassis;
        private readonly List<TriggerChain> _leftClickChains = new();
        private readonly List<TriggerChain> _rightClickChains = new();
        private readonly List<TriggerChain> _timerChains = new();
        private bool _initialized;

        public override void OnNetworkSpawn()
        {
            _chassis = GetComponent<PlayerWeaponChassis>();
            if (_chassis == null) return;

            _chassis.OnGraphUpdated += RebuildFromGraph;
            RebuildFromGraph(_chassis.GetNodeGraph());
        }

        public override void OnNetworkDespawn()
        {
            if (_chassis != null)
                _chassis.OnGraphUpdated -= RebuildFromGraph;
        }

        public void RebuildFromGraph(NodeGraphData graph)
        {
            _leftClickChains.Clear();
            _rightClickChains.Clear();
            _timerChains.Clear();
            _initialized = false;

            if (graph == null || graph.nodes.Count == 0 || moduleRegistry == null || _chassis == null)
            {
                Debug.Log($"[PlayerWeaponExecutor] RebuildFromGraph early exit: graph={graph != null}, nodes={graph?.nodes.Count ?? 0}, registry={moduleRegistry != null}, chassis={_chassis != null}");
                return;
            }

            foreach (var node in graph.nodes)
            {
                if (node.category != ModuleCategory.Trigger) continue;

                var triggerDef = moduleRegistry.GetById(node.moduleDefId) as TriggerDefinition;
                if (triggerDef == null) continue;

                var triggers = ModuleFactory.CreateTriggers(triggerDef, _chassis);
                if (triggers.Count == 0) continue;

                foreach (var triggerInstance in triggers)
                {
                    var chain = new TriggerChain { Trigger = triggerInstance };
                    BuildZoneChain(graph, node.nodeId, chain.ZoneChains);
                    triggerInstance.OnTriggered += () => ExecuteChain(chain);

                    if (triggerInstance is OnLeftClickTrigger)
                        _leftClickChains.Add(chain);
                    else if (triggerInstance is OnRightClickTrigger)
                        _rightClickChains.Add(chain);
                    else
                        _timerChains.Add(chain);
                }
            }

            _initialized = _leftClickChains.Count > 0 || _rightClickChains.Count > 0 || _timerChains.Count > 0;
            Debug.Log($"[PlayerWeaponExecutor] Rebuilt: L={_leftClickChains.Count} R={_rightClickChains.Count} T={_timerChains.Count}");
        }

        private const int MaxChainDepth = 8;

        private void BuildZoneChain(NodeGraphData graph, string fromNodeId, List<ZoneChain> zoneChains, int depth = 0)
        {
            if (depth >= MaxChainDepth) return;

            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;

                var targetNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (targetNode == null) continue;

                if (targetNode.category == ModuleCategory.Zone)
                {
                    var zoneDef = moduleRegistry.GetById(targetNode.moduleDefId) as ZoneDefinition;
                    if (zoneDef == null) continue;

                    var zones = ModuleFactory.CreateZones(zoneDef, _chassis);
                    if (zones.Count == 0) continue;

                    var zoneChain = new ZoneChain { Zone = zones[0] };
                    CollectEffects(graph, targetNode.nodeId, zoneChain.Effects);
                    BuildZoneChain(graph, targetNode.nodeId, zoneChain.ChainedZones, depth + 1);
                    zoneChains.Add(zoneChain);
                }
            }
        }

        private void CollectEffects(NodeGraphData graph, string fromNodeId, List<EffectInstance> effects, int depth = 0)
        {
            if (depth >= MaxChainDepth) return;

            foreach (var conn in graph.connections)
            {
                if (conn.fromNodeId != fromNodeId) continue;
                var effNode = graph.nodes.Find(n => n.nodeId == conn.toNodeId);
                if (effNode?.category != ModuleCategory.Effect) continue;

                var effDef = moduleRegistry.GetById(effNode.moduleDefId) as EffectDefinition;
                if (effDef == null) continue;

                var effInstances = ModuleFactory.CreateEffects(effDef, _chassis);
                effects.AddRange(effInstances);
                CollectEffects(graph, effNode.nodeId, effects, depth + 1);
            }
        }

        private void Update()
        {
            if (!IsServer || !_initialized) return;

            foreach (var chain in _timerChains)
            {
                chain.Trigger.Tick(Time.deltaTime);
            }
        }

        [Rpc(SendTo.Server)]
        public void FireLeftClickRpc()
        {
            Debug.Log($"[PlayerWeaponExecutor] FireLeftClick: {_leftClickChains.Count} chains");
            foreach (var chain in _leftClickChains)
            {
                chain.Trigger.ExternalFire();
            }
        }

        [Rpc(SendTo.Server)]
        public void FireRightClickRpc()
        {
            Debug.Log($"[PlayerWeaponExecutor] FireRightClick: {_rightClickChains.Count} chains");
            foreach (var chain in _rightClickChains)
            {
                chain.Trigger.ExternalFire();
            }
        }

        private void ExecuteChain(TriggerChain chain)
        {
            if (_chassis?.FirePoint == null) return;

            var origin = _chassis.FirePoint.position;
            var range = _chassis.BaseRange;

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
