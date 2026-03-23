using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class CableRenderer : VisualElement
    {
        private readonly List<(Vector2 from, Vector2 to, Color color, bool vertical)> _cables = new();

        private const int FlowSamples = 64;
        private const float DashLength = 12f;
        private const float GapLength = 28f;
        private const float FlowSpeed = 40f;
        private const float FlowWidth = 2.5f;
        private const float FlowAlpha = 0.7f;
        private const float BaseAlpha = 0.35f;

        private float _time;
        private IVisualElementScheduledItem _animSchedule;
        private bool _animated;

        private readonly Vector2[] _pointsCache = new Vector2[FlowSamples + 1];
        private readonly float[] _distancesCache = new float[FlowSamples + 1];

        public CableRenderer(bool animated = false)
        {
            _animated = animated;
            generateVisualContent += OnGenerateVisualContent;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;

            if (_animated)
            {
                RegisterCallback<AttachToPanelEvent>(_ => StartAnimation());
                RegisterCallback<DetachFromPanelEvent>(_ => StopAnimation());
            }
        }

        private void StartAnimation()
        {
            _animSchedule ??= schedule.Execute(() =>
            {
                _time += 0.016f;
                if (_cables.Count > 0) MarkDirtyRepaint();
            }).Every(16);
        }

        private void StopAnimation()
        {
            _animSchedule?.Pause();
            _animSchedule = null;
        }

        public void AddCable(Vector2 from, Vector2 to, Color color, bool vertical = false)
        {
            _cables.Add((from, to, color, vertical));
            MarkDirtyRepaint();
        }

        public new void Clear()
        {
            _cables.Clear();
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
                    cp1 = from + new Vector2(0, dist);
                    cp2 = to - new Vector2(0, dist);
                }
                else
                {
                    dist = Mathf.Abs(to.x - from.x) * 0.5f;
                    dist = Mathf.Max(dist, 30f);
                    cp1 = from + Vector2.right * dist;
                    cp2 = to + Vector2.left * dist;
                }

                var baseColor = _animated
                    ? new Color(color.r, color.g, color.b, BaseAlpha)
                    : color;

                painter.strokeColor = baseColor;
                painter.lineWidth = 2f;
                painter.lineCap = LineCap.Round;
                painter.BeginPath();
                painter.MoveTo(from);
                painter.BezierCurveTo(cp1, cp2, to);
                painter.Stroke();

                if (_animated)
                    DrawFlowDashes(painter, from, cp1, cp2, to, color);

                DrawDiamond(painter, from, 3f, color);
                DrawDiamond(painter, to, 3f, color);
            }
        }

        private void DrawFlowDashes(Painter2D painter, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            var points = _pointsCache;
            var distances = _distancesCache;
            float totalLength = 0f;

            points[0] = p0;
            distances[0] = 0f;

            for (int i = 1; i <= FlowSamples; i++)
            {
                float t = i / (float)FlowSamples;
                float u = 1f - t;
                points[i] = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
                totalLength += Vector2.Distance(points[i - 1], points[i]);
                distances[i] = totalLength;
            }

            if (totalLength < 1f) return;

            float cycle = DashLength + GapLength;
            float offset = cycle - (_time * FlowSpeed) % cycle;

            var flowColor = new Color(color.r, color.g, color.b, FlowAlpha);
            painter.strokeColor = flowColor;
            painter.lineWidth = FlowWidth;
            painter.lineCap = LineCap.Round;

            float dashStart = -offset;
            while (dashStart < totalLength)
            {
                float dStart = Mathf.Max(dashStart, 0f);
                float dEnd = Mathf.Min(dashStart + DashLength, totalLength);

                if (dEnd > dStart + 0.5f)
                {
                    painter.BeginPath();
                    painter.MoveTo(LerpOnCurve(points, distances, dStart));
                    for (int i = 1; i < points.Length; i++)
                    {
                        if (distances[i] < dStart) continue;
                        if (distances[i] > dEnd)
                        {
                            painter.LineTo(LerpOnCurve(points, distances, dEnd));
                            break;
                        }
                        painter.LineTo(points[i]);
                    }
                    if (distances[points.Length - 1] <= dEnd)
                        painter.LineTo(LerpOnCurve(points, distances, dEnd));
                    painter.Stroke();
                }

                dashStart += cycle;
            }
        }

        private static Vector2 LerpOnCurve(Vector2[] points, float[] distances, float dist)
        {
            for (int i = 1; i < points.Length; i++)
            {
                if (distances[i] >= dist)
                {
                    float segLen = distances[i] - distances[i - 1];
                    if (segLen < 0.001f) return points[i];
                    float t = (dist - distances[i - 1]) / segLen;
                    return Vector2.Lerp(points[i - 1], points[i], t);
                }
            }
            return points[points.Length - 1];
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
