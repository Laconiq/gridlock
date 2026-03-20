using System.Collections.Generic;
using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class MinimapWidget
    {
        private readonly VisualElement _canvas;
        private readonly VisualElement _viewportRect;

        public MinimapWidget(VisualElement canvasArea)
        {
            _canvas = canvasArea.Q("viewport-nav-canvas");
            if (_canvas == null) return;

            _viewportRect = new VisualElement();
            _viewportRect.AddToClassList("viewport-nav__viewport-rect");
            _canvas.Add(_viewportRect);
        }

        public bool IsValid => _canvas != null;

        public void Refresh(IReadOnlyList<NodeWidget> nodes, Vector2 panOffset, Rect viewportRect)
        {
            if (_canvas == null) return;

            for (int i = _canvas.childCount - 1; i >= 0; i--)
            {
                if (_canvas[i] != _viewportRect)
                    _canvas[i].RemoveFromHierarchy();
            }

            if (nodes.Count == 0)
            {
                _viewportRect.style.display = DisplayStyle.None;
                return;
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var w in nodes)
            {
                var p = w.Position;
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x + DesignConstants.NodeWidth > maxX) maxX = p.x + DesignConstants.NodeWidth;
                if (p.y + DesignConstants.NodeHeight > maxY) maxY = p.y + DesignConstants.NodeHeight;
            }

            float padding = 200f;
            minX -= padding; minY -= padding;
            maxX += padding; maxY += padding;

            float worldW = maxX - minX;
            float worldH = maxY - minY;
            if (worldW < 1f || worldH < 1f) return;

            var mapRect = _canvas.contentRect;
            if (mapRect.width <= 0 || mapRect.height <= 0) return;

            float scale = Mathf.Min(mapRect.width / worldW, mapRect.height / worldH);

            foreach (var w in nodes)
            {
                var dot = new VisualElement();
                dot.AddToClassList("viewport-nav__node-dot");

                var catClass = w.Data.category switch
                {
                    ModuleCategory.Trigger => "viewport-nav__node-dot--trigger",
                    ModuleCategory.Zone => "viewport-nav__node-dot--zone",
                    ModuleCategory.Effect => "viewport-nav__node-dot--effect",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(catClass))
                    dot.AddToClassList(catClass);

                dot.style.left = (w.Position.x - minX) * scale;
                dot.style.top = (w.Position.y - minY) * scale;
                _canvas.Add(dot);
            }

            _viewportRect.style.display = DisplayStyle.Flex;
            _viewportRect.BringToFront();

            _viewportRect.style.left = (-panOffset.x - minX) * scale;
            _viewportRect.style.top = (-panOffset.y - minY) * scale;
            _viewportRect.style.width = viewportRect.width * scale;
            _viewportRect.style.height = viewportRect.height * scale;
        }
    }
}
