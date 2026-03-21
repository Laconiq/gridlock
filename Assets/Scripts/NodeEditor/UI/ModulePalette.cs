using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using AIWE.Player;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class ModulePalette
    {
        private readonly ModuleRegistry _moduleRegistry;
        private readonly NodeEditorCanvas _canvas;
        private readonly ScrollView _scrollView;
        private readonly Button _tabTriggers;
        private readonly Button _tabZones;
        private readonly Button _tabEffects;

        private PlayerInventory _inventory;
        private ModuleCategory _activeCategory = ModuleCategory.Trigger;
        private readonly Dictionary<string, ModulePaletteItem> _itemsByModuleId = new();

        public ModulePalette(VisualElement root, ModuleRegistry registry, NodeEditorCanvas canvas)
        {
            _moduleRegistry = registry;
            _canvas = canvas;

            _scrollView = root.Q<ScrollView>("palette-scroll");
            _tabTriggers = root.Q<Button>("tab-triggers");
            _tabZones = root.Q<Button>("tab-targets");
            _tabEffects = root.Q<Button>("tab-effects");

            _tabTriggers?.RegisterCallback<ClickEvent>(_ => SetCategory(ModuleCategory.Trigger));
            _tabZones?.RegisterCallback<ClickEvent>(_ => SetCategory(ModuleCategory.Zone));
            _tabEffects?.RegisterCallback<ClickEvent>(_ => SetCategory(ModuleCategory.Effect));
        }

        public void Initialize(PlayerInventory inventory)
        {
            _inventory = inventory;
            _activeCategory = ModuleCategory.Trigger;
            UpdateTabVisuals();
            PopulateModules();
        }

        private void SetCategory(ModuleCategory category)
        {
            _activeCategory = category;
            UpdateTabVisuals();
            PopulateModules();
        }

        private void UpdateTabVisuals()
        {
            UpdateTab(_tabTriggers, ModuleCategory.Trigger);
            UpdateTab(_tabZones, ModuleCategory.Zone);
            UpdateTab(_tabEffects, ModuleCategory.Effect);
        }

        private void UpdateTab(Button tab, ModuleCategory cat)
        {
            if (tab == null) return;
            if (_activeCategory == cat)
                tab.AddToClassList("sidebar__tab--active");
            else
                tab.RemoveFromClassList("sidebar__tab--active");
        }

        private void PopulateModules()
        {
            if (_scrollView == null || _moduleRegistry == null) return;

            _scrollView.Clear();
            _itemsByModuleId.Clear();

            foreach (var moduleDef in _moduleRegistry.AllModules)
            {
                if (moduleDef.category != _activeCategory) continue;

                int count = _inventory != null ? _inventory.GetCount(moduleDef.moduleId) : -1;
                if (_inventory != null && count <= 0) continue;

                var item = new ModulePaletteItem(moduleDef, _canvas, count);
                _scrollView.Add(item.Element);
                _itemsByModuleId[moduleDef.moduleId] = item;
            }
        }

        public void OnNodeAdded(string moduleDefId)
        {
            if (_itemsByModuleId.TryGetValue(moduleDefId, out var item))
                item.AdjustCount(-1);
        }

        public void OnNodeRemoved(string moduleDefId)
        {
            if (_itemsByModuleId.TryGetValue(moduleDefId, out var item))
                item.AdjustCount(1);
        }
    }
}
