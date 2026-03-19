using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    [CreateAssetMenu(menuName = "AIWE/Modules/Trigger")]
    public class TriggerDefinition : ModuleDefinition
    {
        public float defaultCooldown = 1f;

        private void OnValidate()
        {
            category = ModuleCategory.Trigger;
            if (nodeColor == Color.gray)
                nodeColor = new Color(1f, 0.6f, 0.2f);
        }
    }
}
