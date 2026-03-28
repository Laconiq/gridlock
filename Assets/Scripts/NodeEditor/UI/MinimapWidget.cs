using System.Collections.Generic;
using Gridlock.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.NodeEditor.UI
{
    public class MinimapWidget
    {
        private readonly VisualElement _canvas;
        private readonly VisualElement _viewportRect;
        private readonly List<VisualElement> _dotPool = new();
        private int _activeDotCount;

        public MinimapWidget(VisualElement canvasArea)
        {
            _canvas = canvasArea.Q("viewport-nav-canvas");
            if (_canvas == null) return;

            _viewportRect = new VisualElement();
            _viewportRect.AddToClassList("viewport-nav__viewport-rect");
            _canvas.Add(_viewportRect);
        }

        public bool IsValid => _canvas != null;

        private VisualElement GetOrCreateDot(int index)
        {
            if (index < _dotPool.Count)
            {
                var dot = _dotPool[index];
                dot.style.display = DisplayStyle.Flex;
                return dot;
            }

            var newDot = new VisualElement();
            newDot.AddToClassList("viewport-nav__node-dot");
            _canvas.Add(newDot);
            _dotPool.Add(newDot);
            return newDot;
        }

        private void HideExtraDots(int usedCount)
        {
            for (int i = usedCount; i < _activeDotCount; i++)
                _dotPool[i].style.display = DisplayStyle.None;
            _activeDotCount = usedCount;
        }

        public void Refresh(IReadOnlyList<NodeWidget> nodes, Vector2 panOffset, Rect viewportRect, float zoom = 1f)
        {
            if (_canvas == null) return;

            if (nodes.Count == 0)
            {
                HideExtraDots(0);
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

            for (int i = 0; i < nodes.Count; i++)
            {
                var w = nodes[i];
                var dot = GetOrCreateDot(i);

                dot.RemoveFromClassList("viewport-nav__node-dot--trigger");
                dot.RemoveFromClassList("viewport-nav__node-dot--target");
                dot.RemoveFromClassList("viewport-nav__node-dot--effect");

                var catClass = w.Data.category switch
                {
                    ModuleCategory.Trigger => "viewport-nav__node-dot--trigger",
                    ModuleCategory.Zone => "viewport-nav__node-dot--target",
                    ModuleCategory.Effect => "viewport-nav__node-dot--effect",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(catClass))
                    dot.AddToClassList(catClass);

                dot.style.left = (w.Position.x - minX) * scale;
                dot.style.top = (w.Position.y - minY) * scale;
            }

            HideExtraDots(nodes.Count);

            _viewportRect.style.display = DisplayStyle.Flex;
            _viewportRect.BringToFront();

            float viewOriginX = -panOffset.x / zoom;
            float viewOriginY = -panOffset.y / zoom;
            float viewW = viewportRect.width / zoom;
            float viewH = viewportRect.height / zoom;

            _viewportRect.style.left = (viewOriginX - minX) * scale;
            _viewportRect.style.top = (viewOriginY - minY) * scale;
            _viewportRect.style.width = viewW * scale;
            _viewportRect.style.height = viewH * scale;
        }
    }
}
