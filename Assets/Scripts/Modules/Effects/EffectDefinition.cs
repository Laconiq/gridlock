using System.Collections.Generic;
using AIWE.NodeEditor.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    [CreateAssetMenu(menuName = "AIWE/Modules/Effect")]
    public class EffectDefinition : ModuleDefinition
    {
        [SerializeReference, ListDrawerSettings(ShowFoldout = true), InlineProperty]
        public List<EffectInstance> effects = new();

        private void OnValidate()
        {
            category = ModuleCategory.Effect;
            if (nodeColor == Color.gray)
                nodeColor = new Color(0.3f, 0.85f, 0.4f);
        }
    }
}
