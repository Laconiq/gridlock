using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Modules;
using AIWE.Modules.Triggers;
using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Towers
{
    public class TowerExecutor : MonoBehaviour
    {
        [SerializeField] private ModuleRegistry moduleRegistry;

        private IChassis _chassis;
        private readonly List<TriggerChain> _triggerChains = new();
        private bool _initialized;

        private void Start()
        {
            _chassis = GetComponent<IChassis>();

            var towerChassis = GetComponent<TowerChassis>();
            if (towerChassis != null)
                RebuildFromGraph(towerChassis.GetNodeGraph());
        }

        private void OnDestroy()
        {
            ClearChains();
        }

        private void ClearChains()
        {
            _triggerChains.Clear();
            _initialized = false;
        }

        public void RebuildFromGraph(NodeGraphData graph)
        {
            ClearChains();

            if (graph == null || graph.nodes.Count == 0 || moduleRegistry == null || _chassis == null)
                return;

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
                    ChainBuilder.BuildZoneChains(graph, node.nodeId, chain.ZoneChains, moduleRegistry, _chassis);
                    chain.Trigger.OnTriggered += () => ExecuteChain(chain);
                    _triggerChains.Add(chain);
                }
            }

            _initialized = _triggerChains.Count > 0;
            Debug.Log($"[TowerExecutor] Built {_triggerChains.Count} trigger chains on {gameObject.name}");
        }

        private void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;
            foreach (var chain in _triggerChains)
            {
                chain.Trigger.Tick(dt);
                ChainBuilder.TickCooldowns(chain.ZoneChains, dt);
            }
        }

        private void ExecuteChain(TriggerChain chain)
        {
            if (_chassis?.FirePoint == null) return;

            var origin = _chassis.FirePoint.position;
            var range = _chassis.BaseRange;

            foreach (var zoneChain in chain.ZoneChains)
                ExecuteZoneChainWithRotation(zoneChain, origin, range);
        }

        private void ExecuteZoneChainWithRotation(ZoneChain zoneChain, Vector3 origin, float range)
        {
            if (!zoneChain.Zone.IsReady) return;

            var targets = zoneChain.Zone.SelectTargets(origin, range);
            zoneChain.Zone.StartCooldown();

            if (targets.Count > 0 && _chassis?.FirePoint != null)
            {
                var lookDir = (targets[0].Position - origin).normalized;
                if (lookDir.sqrMagnitude > 0.01f)
                    _chassis.FirePoint.rotation = Quaternion.LookRotation(lookDir);
            }

            foreach (var effect in zoneChain.Effects)
            {
                if (!effect.IsReady) continue;
                effect.Execute(targets, origin);
                effect.StartCooldown();
            }

            foreach (var chained in zoneChain.ChainedZones)
                ExecuteZoneChainWithRotation(chained, origin, range);
        }
    }
}
