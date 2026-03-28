using Gridlock.Interfaces;
using Gridlock.NodeEditor.Data;
using UnityEngine;

namespace Gridlock.Towers
{
    public class TowerChassis : MonoBehaviour, IChassis
    {
        [SerializeField] private ChassisDefinition definition;
        [SerializeField] private Transform firePoint;

        private string _serializedGraph;
        private NodeGraphData _cachedGraph;

        public ChassisDefinition Definition => definition;
        public int MaxTriggers => definition != null ? definition.maxTriggers : 1;
        public Transform FirePoint => firePoint;
        public float BaseRange => definition != null ? definition.baseRange : 10f;

        private void Start()
        {
            if (!string.IsNullOrEmpty(_serializedGraph))
            {
                _cachedGraph = GraphSerializer.Deserialize(_serializedGraph);
                var executor = GetComponent<TowerExecutor>();
                if (executor != null)
                    executor.RebuildFromGraph(_cachedGraph);
            }
        }

        public NodeGraphData GetNodeGraph()
        {
            return _cachedGraph ?? new NodeGraphData();
        }

        public void SetNodeGraph(NodeGraphData graph)
        {
            _cachedGraph = graph;
            _serializedGraph = GraphSerializer.Serialize(graph);
            Debug.Log($"[TowerChassis] Graph updated on {gameObject.name}");

            var executor = GetComponent<TowerExecutor>();
            if (executor != null)
                executor.RebuildFromGraph(_cachedGraph);
        }
    }
}
