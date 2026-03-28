using Gridlock.NodeEditor.Data;
using UnityEngine;

namespace Gridlock.Interfaces
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
