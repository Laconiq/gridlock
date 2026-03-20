using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class CableRenderer : VisualElement
    {
        private readonly List<(Vector2 from, Vector2 to, Color color, bool vertical)> _cables = new();

        public CableRenderer()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;
        }

        public void AddCable(Vector2 from, Vector2 to, Color color, bool vertical = false)
        {
            _cables.Add((from, to, color, vertical));
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            foreach (var (from, to, color, vertical) in _cables)
            {
                float dist;
                Vector2 cp1, cp2;

                if (vertical)
                {
                    dist = Mathf.Abs(to.y - from.y) * 0.45f;
                    dist = Mathf.Max(dist, 25f);
                    cp1 = from + Vector2.down * dist;
                    cp2 = to + Vector2.up * dist;
                }
                else
                {
                    dist = Mathf.Abs(to.x - from.x) * 0.5f;
                    dist = Mathf.Max(dist, 30f);
                    cp1 = from + Vector2.right * dist;
                    cp2 = to + Vector2.left * dist;
                }

                painter.strokeColor = color;
                painter.lineWidth = 2f;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(from);
                painter.BezierCurveTo(cp1, cp2, to);
                painter.Stroke();

                float s = 3f;
                painter.fillColor = color;
                foreach (var pt in new[] { from, to })
                {
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(pt.x, pt.y - s));
                    painter.LineTo(new Vector2(pt.x + s, pt.y));
                    painter.LineTo(new Vector2(pt.x, pt.y + s));
                    painter.LineTo(new Vector2(pt.x - s, pt.y));
                    painter.ClosePath();
                    painter.Fill();
                }
            }
        }
    }
}
