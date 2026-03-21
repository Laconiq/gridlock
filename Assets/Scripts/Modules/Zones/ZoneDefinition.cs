using System.Collections.Generic;
using AIWE.NodeEditor.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [CreateAssetMenu(menuName = "AIWE/Modules/Zone")]
    public class ZoneDefinition : ModuleDefinition
    {
        [SerializeReference, ListDrawerSettings(ShowFoldout = true), InlineProperty]
        public List<ZoneInstance> zones = new();

        public float defaultRange = 10f;

        public override float GetCooldown() => zones.Count > 0 ? zones[0].Cooldown : 0f;

        private void OnValidate()
        {
            category = ModuleCategory.Zone;
            if (nodeColor == Color.gray)
                nodeColor = new Color(0.3f, 0.5f, 1f);
        }
    }
}
