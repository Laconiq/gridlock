using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using AIWE.Player;
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
        private PlayerInventory _inventory;
        private readonly Dictionary<string, ModulePaletteItem> _itemsByModuleId = new();

        public void Initialize(NodeEditorCanvas canvas, PlayerInventory inventory)
        {
            _canvas = canvas;
            _inventory = inventory;

            if (_canvas != null)
            {
                _canvas.OnNodeAdded -= OnNodeAdded;
                _canvas.OnNodeRemoved -= OnNodeRemoved;
                _canvas.OnNodeAdded += OnNodeAdded;
                _canvas.OnNodeRemoved += OnNodeRemoved;
            }

            PopulateModules();
        }

        private void PopulateModules()
        {
            ClearSection(triggerSection);
            ClearSection(zoneSection);
            ClearSection(effectSection);
            _itemsByModuleId.Clear();

            if (moduleRegistry == null || paletteItemPrefab == null) return;

            foreach (var moduleDef in moduleRegistry.AllModules)
            {
                int count = _inventory != null ? _inventory.GetCount(moduleDef.moduleId) : -1;

                if (_inventory != null && count <= 0) continue;

                var parent = moduleDef.category switch
                {
                    ModuleCategory.Trigger => triggerSection,
                    ModuleCategory.Zone => zoneSection,
                    ModuleCategory.Effect => effectSection,
                    _ => null
                };

                if (parent == null) continue;

                var item = Instantiate(paletteItemPrefab, parent);
                item.Initialize(moduleDef, _canvas, count);
                _itemsByModuleId[moduleDef.moduleId] = item;
            }
        }

        private void OnNodeAdded(string moduleDefId)
        {
            if (_itemsByModuleId.TryGetValue(moduleDefId, out var item))
                item.AdjustCount(-1);
        }

        private void OnNodeRemoved(string moduleDefId)
        {
            if (_itemsByModuleId.TryGetValue(moduleDefId, out var item))
                item.AdjustCount(1);
        }

        private void ClearSection(Transform section)
        {
            if (section == null) return;
            for (int i = section.childCount - 1; i >= 0; i--)
            {
                Destroy(section.GetChild(i).gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                _canvas.OnNodeAdded -= OnNodeAdded;
                _canvas.OnNodeRemoved -= OnNodeRemoved;
            }
        }
    }
}
