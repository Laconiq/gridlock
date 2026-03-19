using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIWE.NodeEditor.Data
{
    [Serializable]
    public class NodeGraphData
    {
        public List<NodeData> nodes = new();
        public List<ConnectionData> connections = new();
    }

    [Serializable]
    public class NodeData
    {
        public string nodeId;
        public string moduleDefId;
        public ModuleCategory category;
        public Vector2 editorPosition;
        public List<ParamOverride> paramOverrides = new();

        public NodeData()
        {
            nodeId = Guid.NewGuid().ToString();
        }
    }

    [Serializable]
    public class ConnectionData
    {
        public string fromNodeId;
        public string toNodeId;
        public int fromPort;
        public int toPort;
    }

    [Serializable]
    public class ParamOverride
    {
        public string paramName;
        public float value;
    }

    public enum ModuleCategory
    {
        Trigger,
        Zone,
        Effect
    }
}
