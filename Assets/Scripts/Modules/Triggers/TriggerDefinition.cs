using System.Collections.Generic;
using AIWE.NodeEditor.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AIWE.Modules.Triggers
{
    [CreateAssetMenu(menuName = "AIWE/Modules/Trigger")]
    public class TriggerDefinition : ModuleDefinition
    {
        [SerializeReference, ListDrawerSettings(ShowFoldout = true), InlineProperty]
        public List<TriggerInstance> triggers = new();

        private void OnValidate()
        {
            category = ModuleCategory.Trigger;
            if (nodeColor == Color.gray)
                nodeColor = new Color(1f, 0.6f, 0.2f);
        }
    }
}
