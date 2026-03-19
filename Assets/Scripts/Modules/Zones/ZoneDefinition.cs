using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [CreateAssetMenu(menuName = "AIWE/Modules/Zone")]
    public class ZoneDefinition : ModuleDefinition
    {
        public float defaultRange = 10f;
        public bool isAoE;

        private void OnValidate()
        {
            category = ModuleCategory.Zone;
            if (nodeColor == Color.gray)
                nodeColor = new Color(0.3f, 0.5f, 1f);
        }
    }
}
