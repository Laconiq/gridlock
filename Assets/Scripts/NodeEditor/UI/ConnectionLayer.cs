using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class ConnectionLayer : VisualElement
    {
        private readonly List<(VisualElement from, VisualElement to, Color color)> _connections = new();

        private VisualElement _tempFrom;
        private Vector2 _tempEnd;
        private Color _tempColor;
        private bool _hasTempConnection;

        public ConnectionLayer()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;
        }

        public void SetConnections(List<(VisualElement from, VisualElement to, Color color)> connections)
        {
            _connections.Clear();
            _connections.AddRange(connections);
            MarkDirtyRepaint();
        }

        public void ClearConnections()
        {
            _connections.Clear();
            MarkDirtyRepaint();
        }

        public void SetTempConnection(VisualElement from, Vector2 endLocalPos, Color color)
        {
            _tempFrom = from;
            _tempEnd = endLocalPos;
            _tempColor = color;
            _hasTempConnection = true;
            MarkDirtyRepaint();
        }

        public void ClearTempConnection()
        {
            _hasTempConnection = false;
            _tempFrom = null;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;

            foreach (var (from, to, color) in _connections)
            {
                if (from == null || to == null) continue;
                var start = this.WorldToLocal(from.worldBound.center);
                var end = this.WorldToLocal(to.worldBound.center);
                DrawBezier(painter, start, end, color, 2f);
            }

            if (_hasTempConnection && _tempFrom != null)
            {
                var start = this.WorldToLocal(_tempFrom.worldBound.center);
                DrawBezier(painter, start, _tempEnd, _tempColor, 2f, 0.5f);
            }
        }

        private void DrawBezier(Painter2D painter, Vector2 start, Vector2 end, Color color, float width, float alpha = 1f)
        {
            var drawColor = new Color(color.r, color.g, color.b, color.a * alpha);

            float dist = Mathf.Abs(end.x - start.x) * 0.5f;
            dist = Mathf.Max(dist, 50f);

            var cp1 = start + Vector2.right * dist;
            var cp2 = end + Vector2.left * dist;

            painter.strokeColor = drawColor;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(cp1, cp2, end);
            painter.Stroke();

            DrawDiamond(painter, start, 3f, drawColor);
            DrawDiamond(painter, end, 3f, drawColor);
        }

        private void DrawDiamond(Painter2D painter, Vector2 center, float size, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x, center.y - size));
            painter.LineTo(new Vector2(center.x + size, center.y));
            painter.LineTo(new Vector2(center.x, center.y + size));
            painter.LineTo(new Vector2(center.x - size, center.y));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
