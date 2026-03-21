using System.Collections.Generic;
using AIWE.Modules;
using AIWE.Modules.Effects;
using AIWE.Modules.Triggers;
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
                    ChainBuilder.BuildZoneChains(graph, node.nodeId, chain.ZoneChains, moduleRegistry, _chassis);
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

        private void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;

            if (IsServer)
            {
                foreach (var chain in _timerChains)
                    chain.Trigger.Tick(dt);
            }

            TickAllCooldowns(dt);
        }

        private void TickAllCooldowns(float dt)
        {
            void TickChains(List<TriggerChain> chains)
            {
                foreach (var chain in chains)
                    ChainBuilder.TickCooldowns(chain.ZoneChains, dt);
            }

            TickChains(_leftClickChains);
            TickChains(_rightClickChains);
            TickChains(_timerChains);
        }

        private Vector3 _serverOrigin;
        private Vector3 _serverAimDir;
        private bool _ownerAlreadySpawnedVisual;

        [Rpc(SendTo.Server)]
        public void FireLeftClickRpc(Vector3 origin, Vector3 aimDirection)
        {
            SetServerAim(origin, aimDirection);
            bool fired = TryFireChains(_leftClickChains);
            if (fired) BroadcastVisualProjectileRpc(origin, aimDirection, false);
        }

        [Rpc(SendTo.Server)]
        public void FireRightClickRpc(Vector3 origin, Vector3 aimDirection)
        {
            SetServerAim(origin, aimDirection);
            bool fired = TryFireChains(_rightClickChains);
            if (fired) BroadcastVisualProjectileRpc(origin, aimDirection, true);
        }

        private bool TryFireChains(List<TriggerChain> chains)
        {
            bool anyFired = false;
            foreach (var chain in chains)
            {
                if (HasReadyEffects(chain))
                {
                    chain.Trigger.ExternalFire();
                    anyFired = true;
                }
            }
            return anyFired;
        }

        private bool HasReadyEffects(TriggerChain chain)
        {
            foreach (var zc in chain.ZoneChains)
            {
                if (!zc.Zone.IsReady) continue;
                foreach (var eff in zc.Effects)
                    if (eff.IsReady) return true;
            }
            return false;
        }

        private void SetServerAim(Vector3 origin, Vector3 aimDirection)
        {
            _serverOrigin = origin;
            _serverAimDir = aimDirection.sqrMagnitude > 0.01f ? aimDirection : Vector3.forward;
            if (_chassis?.FirePoint != null)
                _chassis.FirePoint.rotation = Quaternion.LookRotation(_serverAimDir);
        }

        public void SpawnLocalProjectile(Vector3 origin, Vector3 direction, bool rightClick = false)
        {
            var chains = rightClick ? _rightClickChains : _leftClickChains;
            if (!HasAnyReady(chains)) return;

            _ownerAlreadySpawnedVisual = true;
            SpawnVisualFromChains(origin, direction, rightClick);
        }

        private bool HasAnyReady(List<TriggerChain> chains)
        {
            foreach (var chain in chains)
                if (HasReadyEffects(chain)) return true;
            return false;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void BroadcastVisualProjectileRpc(Vector3 origin, Vector3 direction, bool rightClick)
        {
            if (IsOwner && _ownerAlreadySpawnedVisual)
            {
                _ownerAlreadySpawnedVisual = false;
                return;
            }

            SpawnVisualFromChains(origin, direction, rightClick);
        }

        private void SpawnVisualFromChains(Vector3 origin, Vector3 direction, bool rightClick = false)
        {
            var chains = rightClick ? _rightClickChains : _leftClickChains;

            foreach (var chain in chains)
            {
                foreach (var zoneChain in chain.ZoneChains)
                {
                    foreach (var effect in zoneChain.Effects)
                    {
                        if (effect is ProjectileEffect proj)
                            ProjectileEffect.SpawnVisual(proj.ProjectilePrefab, origin, direction, proj.Speed);
                    }
                }
            }
        }

        private void ExecuteChain(TriggerChain chain)
        {
            var origin = _serverOrigin != Vector3.zero ? _serverOrigin : (_chassis?.FirePoint?.position ?? transform.position);
            var range = _chassis?.BaseRange ?? 15f;

            foreach (var zoneChain in chain.ZoneChains)
            {
                ChainBuilder.ExecuteZoneChain(zoneChain, origin, range);
            }
        }
    }
}
