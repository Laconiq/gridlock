using System.Collections.Generic;
using System.Linq;
using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.RadialMenu
{
    public class RadialMenuCanvas
    {
        private const int MaxChainDepth = 8;
        private readonly NodeGraphData _graph;

        public RadialMenuCanvas(NodeGraphData graph)
        {
            _graph = graph ?? new NodeGraphData();
        }

        public NodeGraphData Graph => _graph;

        public List<NodeData> GetTriggers()
        {
            return _graph.nodes.Where(n => n.category == ModuleCategory.Trigger).ToList();
        }

        public List<NodeData> GetZonesForTrigger(string triggerNodeId)
        {
            var zones = new List<NodeData>();
            var connections = GetOutgoingConnections(triggerNodeId, 0);
            if (connections.Count == 0) return zones;

            var firstZoneNode = FindNode(connections[0].toNodeId);
            if (firstZoneNode == null || firstZoneNode.category != ModuleCategory.Zone) return zones;

            zones.Add(firstZoneNode);
            var currentId = firstZoneNode.nodeId;

            for (int depth = 0; depth < MaxChainDepth; depth++)
            {
                var next = GetOutgoingConnections(currentId, 0);
                if (next.Count == 0) break;

                var nextNode = FindNode(next[0].toNodeId);
                if (nextNode == null || nextNode.category != ModuleCategory.Zone) break;

                zones.Add(nextNode);
                currentId = nextNode.nodeId;
            }

            return zones;
        }

        public List<NodeData> GetEffectsForZone(string zoneNodeId)
        {
            var effects = new List<NodeData>();
            var connections = GetOutgoingConnections(zoneNodeId, 1);
            if (connections.Count == 0) return effects;

            var firstEffect = FindNode(connections[0].toNodeId);
            if (firstEffect == null || firstEffect.category != ModuleCategory.Effect) return effects;

            effects.Add(firstEffect);
            var currentId = firstEffect.nodeId;

            for (int depth = 0; depth < MaxChainDepth; depth++)
            {
                var next = GetOutgoingConnections(currentId, 0);
                if (next.Count == 0) break;

                var nextNode = FindNode(next[0].toNodeId);
                if (nextNode == null || nextNode.category != ModuleCategory.Effect) break;

                effects.Add(nextNode);
                currentId = nextNode.nodeId;
            }

            return effects;
        }

        public NodeData AddTrigger(string moduleDefId)
        {
            int triggerCount = _graph.nodes.Count(n => n.category == ModuleCategory.Trigger);
            var node = new NodeData
            {
                moduleDefId = moduleDefId,
                category = ModuleCategory.Trigger,
                editorPosition = new Vector2(200f, triggerCount * 400f)
            };
            _graph.nodes.Add(node);
            return node;
        }

        public NodeData AddZone(string moduleDefId, string parentNodeId)
        {
            var tailId = FindChainTail(parentNodeId, 0);
            var tailNode = FindNode(tailId);

            var node = new NodeData
            {
                moduleDefId = moduleDefId,
                category = ModuleCategory.Zone,
                editorPosition = new Vector2(
                    tailNode.editorPosition.x + 300f,
                    tailNode.editorPosition.y)
            };
            _graph.nodes.Add(node);

            _graph.connections.Add(new ConnectionData
            {
                fromNodeId = tailId,
                fromPort = 0,
                toNodeId = node.nodeId,
                toPort = 0
            });

            return node;
        }

        public NodeData AddEffect(string moduleDefId, string parentZoneNodeId)
        {
            var zoneNode = FindNode(parentZoneNodeId);
            var existingEffectConnections = GetOutgoingConnections(parentZoneNodeId, 1);

            int fromPort;
            string attachId;
            float yOffset;

            if (existingEffectConnections.Count == 0)
            {
                attachId = parentZoneNodeId;
                fromPort = 1;
                yOffset = zoneNode.editorPosition.y + 160f;
            }
            else
            {
                var tailId = FindChainTail(existingEffectConnections[0].toNodeId, 0);
                var tailNode = FindNode(tailId);
                attachId = tailId;
                fromPort = 0;
                yOffset = tailNode.editorPosition.y + 160f;
            }

            var attachNode = FindNode(attachId);
            var node = new NodeData
            {
                moduleDefId = moduleDefId,
                category = ModuleCategory.Effect,
                editorPosition = new Vector2(
                    attachNode.editorPosition.x + 10f,
                    yOffset)
            };
            _graph.nodes.Add(node);

            _graph.connections.Add(new ConnectionData
            {
                fromNodeId = attachId,
                fromPort = fromPort,
                toNodeId = node.nodeId,
                toPort = 0
            });

            return node;
        }

        public void RemoveNode(string nodeId)
        {
            _graph.nodes.RemoveAll(n => n.nodeId == nodeId);
            _graph.connections.RemoveAll(c => c.fromNodeId == nodeId || c.toNodeId == nodeId);
        }

        public NodeData FindNode(string nodeId)
        {
            return _graph.nodes.Find(n => n.nodeId == nodeId);
        }

        public List<ConnectionData> GetOutgoingConnections(string nodeId, int fromPort)
        {
            return _graph.connections
                .Where(c => c.fromNodeId == nodeId && c.fromPort == fromPort)
                .ToList();
        }

        public string FindChainTail(string startNodeId, int fromPort)
        {
            var currentId = startNodeId;

            while (true)
            {
                var next = GetOutgoingConnections(currentId, fromPort);
                if (next.Count == 0) break;
                currentId = next[0].toNodeId;
            }

            return currentId;
        }
    }
}
