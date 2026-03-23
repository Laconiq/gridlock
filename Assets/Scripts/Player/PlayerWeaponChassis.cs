using System;
using AIWE.Interfaces;
using AIWE.NodeEditor.Data;
using AIWE.Towers;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerWeaponChassis : NetworkBehaviour, IChassis
    {
        [SerializeField] private ChassisDefinition definition;
        [SerializeField] private Transform firePoint;
        [SerializeField] private DefaultWeaponGraph defaultWeaponGraph;

        private readonly NetworkVariable<FixedString4096Bytes> _serializedGraph = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NodeGraphData _cachedGraph;

        public event Action<NodeGraphData> OnGraphUpdated;

        public ChassisDefinition Definition => definition;
        public int MaxTriggers => definition != null ? definition.maxTriggers : 2;
        public Transform FirePoint => firePoint;
        public float BaseRange => definition != null ? definition.baseRange : 15f;

        public override void OnNetworkSpawn()
        {
            _serializedGraph.OnValueChanged += OnGraphChanged;

            var graphStr = _serializedGraph.Value.ToString();
            if (!string.IsNullOrEmpty(graphStr))
            {
                _cachedGraph = GraphSerializer.Deserialize(graphStr);
            }
            else if (IsServer && defaultWeaponGraph != null && defaultWeaponGraph.graph != null)
            {
                SetNodeGraph(defaultWeaponGraph.graph);
            }
        }

        public override void OnNetworkDespawn()
        {
            _serializedGraph.OnValueChanged -= OnGraphChanged;
        }

        private void OnGraphChanged(FixedString4096Bytes prev, FixedString4096Bytes current)
        {
            _cachedGraph = GraphSerializer.Deserialize(current.ToString());
            Debug.Log($"[PlayerWeapon] Graph updated for {gameObject.name}");
            OnGraphUpdated?.Invoke(_cachedGraph);
        }

        public void ResetToDefault()
        {
            if (!IsServer) return;
            if (defaultWeaponGraph != null && defaultWeaponGraph.graph != null)
                SetNodeGraph(defaultWeaponGraph.graph);
        }

        public NodeGraphData GetNodeGraph()
        {
            return _cachedGraph ?? new NodeGraphData();
        }

        public void SetNodeGraph(NodeGraphData graph)
        {
            _cachedGraph = graph;
            if (IsServer)
            {
                var json = GraphSerializer.Serialize(graph);
                _serializedGraph.Value = new FixedString4096Bytes(json);
            }
            else
            {
                var json = GraphSerializer.Serialize(graph);
                UpdateGraphRpc(json);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void UpdateGraphRpc(string json, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != OwnerClientId)
            {
                Debug.LogWarning($"[PlayerWeapon] Graph update rejected: sender {rpcParams.Receive.SenderClientId} is not owner {OwnerClientId}");
                return;
            }

            if (string.IsNullOrEmpty(json) || json.Length > 4000)
            {
                Debug.LogWarning("[PlayerWeapon] Graph data too large or empty, rejected");
                return;
            }

            var graph = GraphSerializer.Deserialize(json);
            if (graph == null) return;
            _serializedGraph.Value = new FixedString4096Bytes(json);
        }
    }
}
