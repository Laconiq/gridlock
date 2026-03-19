using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.NodeEditor.UI
{
    public class ModulePalette : MonoBehaviour
    {
        [SerializeField] private ModulePaletteItem paletteItemPrefab;
        [SerializeField] private Transform triggerSection;
        [SerializeField] private Transform zoneSection;
        [SerializeField] private Transform effectSection;
        [SerializeField] private ModuleRegistry moduleRegistry;

        private NodeEditorCanvas _canvas;

        public void Initialize(NodeEditorCanvas canvas)
        {
            _canvas = canvas;
            PopulateModules();
        }

        private void PopulateModules()
        {
            ClearSection(triggerSection);
            ClearSection(zoneSection);
            ClearSection(effectSection);

            if (moduleRegistry == null || paletteItemPrefab == null) return;

            foreach (var moduleDef in moduleRegistry.AllModules)
            {
                var parent = moduleDef.category switch
                {
                    ModuleCategory.Trigger => triggerSection,
                    ModuleCategory.Zone => zoneSection,
                    ModuleCategory.Effect => effectSection,
                    _ => null
                };

                if (parent == null) continue;

                var item = Instantiate(paletteItemPrefab, parent);
                item.Initialize(moduleDef, _canvas);
            }
        }

        private void ClearSection(Transform section)
        {
            if (section == null) return;
            for (int i = section.childCount - 1; i >= 0; i--)
            {
                Destroy(section.GetChild(i).gameObject);
            }
        }
    }
}
