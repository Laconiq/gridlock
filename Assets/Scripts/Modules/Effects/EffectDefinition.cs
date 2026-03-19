using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [CreateAssetMenu(menuName = "AIWE/Modules/Effect")]
    public class EffectDefinition : ModuleDefinition
    {
        public float defaultDamage = 10f;
        public float defaultDuration = 1f;

        private void OnValidate()
        {
            category = ModuleCategory.Effect;
            if (nodeColor == Color.gray)
                nodeColor = new Color(0.3f, 0.85f, 0.4f);
        }
    }
}
