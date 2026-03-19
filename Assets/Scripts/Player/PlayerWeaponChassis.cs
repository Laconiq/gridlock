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

        private readonly NetworkVariable<FixedString4096Bytes> _serializedGraph = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NodeGraphData _cachedGraph;

        public ChassisDefinition Definition => definition;
        public int MaxTriggers => definition != null ? definition.maxTriggers : 2;
        public Transform FirePoint => firePoint;

        public override void OnNetworkSpawn()
        {
            _serializedGraph.OnValueChanged += OnGraphChanged;

            if (!string.IsNullOrEmpty(_serializedGraph.Value.ToString()))
            {
                _cachedGraph = GraphSerializer.Deserialize(_serializedGraph.Value.ToString());
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
        private void UpdateGraphRpc(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 4000) return;
            var graph = GraphSerializer.Deserialize(json);
            if (graph == null) return;
            _serializedGraph.Value = new FixedString4096Bytes(json);
        }
    }
}
