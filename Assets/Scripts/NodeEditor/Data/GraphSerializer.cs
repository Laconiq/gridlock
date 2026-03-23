using System;
using UnityEngine;

namespace AIWE.NodeEditor.Data
{
    public static class GraphSerializer
    {
        public static string Serialize(NodeGraphData graph)
        {
            if (graph == null) return "{}";
            return JsonUtility.ToJson(graph);
        }

        public static NodeGraphData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "{}")
                return new NodeGraphData();

            try
            {
                return JsonUtility.FromJson<NodeGraphData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GraphSerializer] Failed to deserialize graph: {e.Message}");
                return new NodeGraphData();
            }
        }
    }
}
