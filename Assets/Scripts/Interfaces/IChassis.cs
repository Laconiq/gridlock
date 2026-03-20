using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Interfaces
{
    public interface IChassis
    {
        NodeGraphData GetNodeGraph();
        void SetNodeGraph(NodeGraphData graph);
        int MaxTriggers { get; }
        Transform FirePoint { get; }
        float BaseRange { get; }
    }
}
