using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Player
{
    [CreateAssetMenu(menuName = "AIWE/Default Weapon Graph")]
    public class DefaultWeaponGraph : ScriptableObject
    {
        public NodeGraphData graph = new();

        [Header("Module IDs")]
        [SerializeField] private string leftClickId = "trigger_onleftclick";
        [SerializeField] private string rightClickId = "trigger_onrightclick";
        [SerializeField] private string zoneId = "forward_aim";
        [SerializeField] private string effectId = "projectile";

        [ContextMenu("Build Default Graph")]
        public void BuildDefault()
        {
            graph = new NodeGraphData();

            var leftTrigger = new NodeData
            {
                nodeId = "default_left_trigger",
                moduleDefId = leftClickId,
                category = ModuleCategory.Trigger,
                editorPosition = new Vector2(-300, 0),
                isFixed = true
            };
            var rightTrigger = new NodeData
            {
                nodeId = "default_right_trigger",
                moduleDefId = rightClickId,
                category = ModuleCategory.Trigger,
                editorPosition = new Vector2(-300, 400),
                isFixed = true
            };
            var zoneLeft = new NodeData
            {
                nodeId = "default_zone_left",
                moduleDefId = zoneId,
                category = ModuleCategory.Zone,
                editorPosition = new Vector2(0, 0)
            };
            var zoneRight = new NodeData
            {
                nodeId = "default_zone_right",
                moduleDefId = zoneId,
                category = ModuleCategory.Zone,
                editorPosition = new Vector2(0, 400)
            };
            var effectLeft = new NodeData
            {
                nodeId = "default_effect_left",
                moduleDefId = effectId,
                category = ModuleCategory.Effect,
                editorPosition = new Vector2(10, 140)
            };
            var effectRight = new NodeData
            {
                nodeId = "default_effect_right",
                moduleDefId = effectId,
                category = ModuleCategory.Effect,
                editorPosition = new Vector2(10, 540)
            };

            graph.nodes.AddRange(new[] { leftTrigger, rightTrigger, zoneLeft, zoneRight, effectLeft, effectRight });

            // LeftClick -> ZoneLeft (horizontal chain)
            graph.connections.Add(new ConnectionData
            {
                fromNodeId = leftTrigger.nodeId,
                toNodeId = zoneLeft.nodeId,
                fromPort = 0,
                toPort = 0
            });
            // ZoneLeft -> EffectLeft (vertical: zone output port 1 -> effect input port 0)
            graph.connections.Add(new ConnectionData
            {
                fromNodeId = zoneLeft.nodeId,
                toNodeId = effectLeft.nodeId,
                fromPort = 1,
                toPort = 0
            });
            // RightClick -> ZoneRight
            graph.connections.Add(new ConnectionData
            {
                fromNodeId = rightTrigger.nodeId,
                toNodeId = zoneRight.nodeId,
                fromPort = 0,
                toPort = 0
            });
            // ZoneRight -> EffectRight (vertical: zone output port 1 -> effect input port 0)
            graph.connections.Add(new ConnectionData
            {
                fromNodeId = zoneRight.nodeId,
                toNodeId = effectRight.nodeId,
                fromPort = 1,
                toPort = 0
            });

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}
