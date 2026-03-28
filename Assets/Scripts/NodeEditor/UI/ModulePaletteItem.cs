using Gridlock.Modules;
using Gridlock.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.NodeEditor.UI
{
    public class ModulePaletteItem
    {
        public VisualElement Element { get; }

        private readonly ModuleDefinition _definition;
        private readonly NodeEditorCanvas _canvas;
        private int _count;
        private readonly bool _hasInventory;
        private readonly Label _nameLabel;
        private readonly Label _countLabel;


        public ModulePaletteItem(ModuleDefinition definition, NodeEditorCanvas canvas, int count)
        {
            _definition = definition;
            _canvas = canvas;
            _hasInventory = count >= 0;
            _count = count;

            Element = new VisualElement();
            Element.AddToClassList("palette-item");

            var catClass = definition.category switch
            {
                ModuleCategory.Trigger => "palette-item--trigger",
                ModuleCategory.Zone => "palette-item--target",
                ModuleCategory.Effect => "palette-item--effect",
                _ => ""
            };
            if (!string.IsNullOrEmpty(catClass))
                Element.AddToClassList(catClass);

            var leftGroup = new VisualElement();
            leftGroup.AddToClassList("palette-item__left");

            var icon = new VisualElement();
            icon.AddToClassList("palette-item__icon");
            var iconPath = DesignConstants.GetIconPath(definition.category switch
            {
                ModuleCategory.Trigger => "trigger",
                ModuleCategory.Zone => "zone",
                ModuleCategory.Effect => "effect",
                _ => ""
            });
            if (iconPath != null)
            {
                var iconTex = Resources.Load<Texture2D>(iconPath);
                if (iconTex != null)
                    icon.style.backgroundImage = new StyleBackground(iconTex);
            }
            leftGroup.Add(icon);

            _nameLabel = new Label();
            _nameLabel.AddToClassList("palette-item__name");
            leftGroup.Add(_nameLabel);

            Element.Add(leftGroup);

            _countLabel = new Label();
            _countLabel.AddToClassList("palette-item__count");
            Element.Add(_countLabel);

            UpdateDisplay();

            Element.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        public void AdjustCount(int delta)
        {
            if (!_hasInventory) return;
            _count = Mathf.Max(0, _count + delta);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _nameLabel.text = _definition.displayName.ToUpper();
            _countLabel.text = _hasInventory ? $"x{_count}" : "";

            if (_hasInventory && _count <= 0)
                Element.AddToClassList("palette-item--depleted");
            else
                Element.RemoveFromClassList("palette-item--depleted");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            if (_hasInventory && _count <= 0) return;
            if (_canvas == null || _definition == null) return;

            var panelPos = new Vector2(evt.position.x, evt.position.y);
            var canvasPos = _canvas.PanelToCanvasPosition(panelPos);
            canvasPos.x -= 96f;
            canvasPos.y -= 20f;

            var nodeData = new NodeData
            {
                moduleDefId = _definition.moduleId,
                category = _definition.category,
                editorPosition = canvasPos
            };

            var node = _canvas.AddNode(nodeData, animated: true);
            if (node != null)
                _canvas.BeginNodeDrag(node, evt);

            evt.StopPropagation();
        }
    }
}
