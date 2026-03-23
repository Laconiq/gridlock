using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Network;
using AIWE.NodeEditor.Data;
using AIWE.Player;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Towers
{
    public class TowerChassis : NetworkBehaviour, IChassis
    {
        [SerializeField] private ChassisDefinition definition;
        [SerializeField] private Transform firePoint;
        [SerializeField] private DefaultWeaponGraph defaultGraph;

        private readonly NetworkVariable<FixedString4096Bytes> _serializedGraph = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NodeGraphData _cachedGraph;

        public ChassisDefinition Definition => definition;
        public int MaxTriggers => definition != null ? definition.maxTriggers : 1;
        public Transform FirePoint => firePoint;
        public float BaseRange => definition != null ? definition.baseRange : 10f;

        public override void OnNetworkSpawn()
        {
            _serializedGraph.OnValueChanged += OnGraphChanged;

            var graphStr = _serializedGraph.Value.ToString();
            if (!string.IsNullOrEmpty(graphStr))
            {
                _cachedGraph = GraphSerializer.Deserialize(graphStr);
            }
            else if (IsServer && defaultGraph != null && defaultGraph.graph != null)
            {
                SetNodeGraph(defaultGraph.graph);
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
        private void UpdateGraphRpc(string json, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null && !lockManager.IsLockedBy(senderId))
            {
                Debug.LogWarning($"[TowerChassis] Graph update rejected: client {senderId} does not hold the lock");
                return;
            }

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
