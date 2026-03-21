using System;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using AIWE.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.RadialMenu
{
    public class AddModulePopup
    {
        private readonly VisualElement _popup;
        private readonly VisualElement _list;
        private readonly ModuleRegistry _registry;

        public event Action<string> OnModuleSelected;

        public AddModulePopup(VisualElement popup, ModuleRegistry registry)
        {
            _popup = popup;
            _list = popup.Q("add-popup-list");
            _registry = registry;
        }

        public void Show(ModuleCategory category, PlayerInventory inventory)
        {
            _list.Clear();

            var borderColor = category switch
            {
                ModuleCategory.Trigger => new Color(1f, 0.47f, 0.28f),
                ModuleCategory.Zone => new Color(0.56f, 0.96f, 1f),
                ModuleCategory.Effect => new Color(0.18f, 0.97f, 0f),
                _ => Color.white
            };
            _popup.style.borderLeftColor = new StyleColor(borderColor);

            foreach (var module in _registry.AllModules)
            {
                if (module.category != category) continue;

                var count = inventory != null ? inventory.GetCount(module.moduleId) : 0;
                var btn = new Button { text = module.displayName.ToUpper() };
                btn.AddToClassList("radial-add-popup__item");

                if (count <= 0)
                {
                    btn.AddToClassList("radial-add-popup__item--unavailable");
                    btn.SetEnabled(false);
                }
                else
                {
                    btn.text += $"  ({count})";
                    string id = module.moduleId;
                    btn.clicked += () =>
                    {
                        OnModuleSelected?.Invoke(id);
                        Hide();
                    };
                }

                _list.Add(btn);
            }

            _popup.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _popup.style.display = DisplayStyle.None;
        }

        public bool IsVisible => _popup.resolvedStyle.display == DisplayStyle.Flex;
    }
}
