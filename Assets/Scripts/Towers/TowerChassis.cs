using AIWE.Interfaces;
using AIWE.NodeEditor.Data;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Towers
{
    public class TowerChassis : NetworkBehaviour, IChassis
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
        public int MaxTriggers => definition != null ? definition.maxTriggers : 1;
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
            var json = current.ToString();
            _cachedGraph = GraphSerializer.Deserialize(json);
            Debug.Log($"[TowerChassis] Graph updated on {gameObject.name}");

            var executor = GetComponent<TowerExecutor>();
            if (executor != null)
                executor.RebuildFromGraph(_cachedGraph);
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
                if (json.Length > 4000)
                {
                    Debug.LogWarning("[TowerChassis] Graph too large, truncated");
                    return;
                }
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
            if (string.IsNullOrEmpty(json) || json.Length > 4000)
            {
                Debug.LogWarning("[TowerChassis] Invalid graph data rejected");
                return;
            }

            var graph = GraphSerializer.Deserialize(json);
            if (graph == null)
            {
                Debug.LogWarning("[TowerChassis] Failed to deserialize graph");
                return;
            }

            _serializedGraph.Value = new FixedString4096Bytes(json);
        }
    }
}
