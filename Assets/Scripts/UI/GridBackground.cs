using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.UI
{
    public class GridBackground : VisualElement
    {
        private const float DefaultGridSize = 40f;

        private readonly float _gridSize;
        private readonly Color _gridColor;
        private Vector2 _offset;
        private float _zoom = 1f;

        private static readonly Color DefaultColor = new(0.56f, 0.96f, 1f, 0.05f);

        public GridBackground(float gridSize = DefaultGridSize, Color? color = null)
        {
            _gridSize = gridSize;
            _gridColor = color ?? DefaultColor;

            generateVisualContent += OnGenerateVisualContent;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;
        }

        public void UpdateTransform(Vector2 offset, float zoom = 1f)
        {
            _offset = offset;
            _zoom = zoom;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var rect = contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            var painter = mgc.painter2D;
            painter.strokeColor = _gridColor;
            painter.lineWidth = 1f;

            float scaledGrid = _gridSize * _zoom;
            if (scaledGrid < 4f) return;

            float startX = _offset.x % scaledGrid;
            if (startX < 0) startX += scaledGrid;

            float startY = _offset.y % scaledGrid;
            if (startY < 0) startY += scaledGrid;

            for (float x = startX; x < rect.width; x += scaledGrid)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, rect.height));
                painter.Stroke();
            }

            for (float y = startY; y < rect.height; y += scaledGrid)
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(rect.width, y));
                painter.Stroke();
            }
        }
    }
}
