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

        private float _time;
        private IVisualElementScheduledItem _animSchedule;

        const int FlowSamples = 64;
        const float DashLength = 12f;
        const float GapLength = 28f;
        const float FlowSpeed = 40f;
        const float FlowWidth = 2.5f;
        const float FlowAlpha = 0.7f;
        const float BaseAlpha = 0.35f;

        public ConnectionLayer()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(_ => StartAnimation());
            RegisterCallback<DetachFromPanelEvent>(_ => StopAnimation());
        }

        private void StartAnimation()
        {
            _animSchedule ??= schedule.Execute(AnimTick).Every(16);
        }

        private void StopAnimation()
        {
            _animSchedule?.Pause();
            _animSchedule = null;
        }

        private void AnimTick()
        {
            _time += 0.016f;
            if (_connections.Count > 0 || _hasTempConnection)
                MarkDirtyRepaint();
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
                DrawCable(painter, start, end, color, 1f);
            }

            if (_hasTempConnection && _tempFrom != null)
            {
                var start = this.WorldToLocal(_tempFrom.worldBound.center);
                DrawCable(painter, start, _tempEnd, _tempColor, 0.5f);
            }
        }

        private void DrawCable(Painter2D painter, Vector2 start, Vector2 end, Color color, float alphaMultiplier)
        {
            float dist = Mathf.Abs(end.x - start.x) * 0.5f;
            dist = Mathf.Max(dist, 50f);
            var cp1 = start + Vector2.right * dist;
            var cp2 = end + Vector2.left * dist;

            var baseColor = new Color(color.r, color.g, color.b, BaseAlpha * alphaMultiplier);
            painter.strokeColor = baseColor;
            painter.lineWidth = 2f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(start);
            painter.BezierCurveTo(cp1, cp2, end);
            painter.Stroke();

            DrawFlowDashes(painter, start, cp1, cp2, end, color, alphaMultiplier);

            var diamondColor = new Color(color.r, color.g, color.b, color.a * alphaMultiplier);
            DrawDiamond(painter, start, 3f, diamondColor);
            DrawDiamond(painter, end, 3f, diamondColor);
        }

        private void DrawFlowDashes(Painter2D painter, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3,
            Color color, float alphaMultiplier)
        {
            var points = new Vector2[FlowSamples + 1];
            var distances = new float[FlowSamples + 1];
            float totalLength = 0f;

            points[0] = p0;
            distances[0] = 0f;

            for (int i = 1; i <= FlowSamples; i++)
            {
                float t = i / (float)FlowSamples;
                points[i] = EvalBezier(p0, p1, p2, p3, t);
                totalLength += Vector2.Distance(points[i - 1], points[i]);
                distances[i] = totalLength;
            }

            if (totalLength < 1f) return;

            float cycle = DashLength + GapLength;
            float offset = cycle - (_time * FlowSpeed) % cycle;

            var flowColor = new Color(color.r, color.g, color.b, FlowAlpha * alphaMultiplier);
            painter.strokeColor = flowColor;
            painter.lineWidth = FlowWidth;
            painter.lineCap = LineCap.Round;

            float dashStart = -offset;
            while (dashStart < totalLength)
            {
                float dStart = dashStart;
                float dEnd = dashStart + DashLength;

                dStart = Mathf.Max(dStart, 0f);
                dEnd = Mathf.Min(dEnd, totalLength);

                if (dEnd > dStart + 0.5f)
                    DrawSegment(painter, points, distances, dStart, dEnd, totalLength);

                dashStart += cycle;
            }
        }

        private void DrawSegment(Painter2D painter, Vector2[] points, float[] distances,
            float startDist, float endDist, float totalLength)
        {
            painter.BeginPath();
            painter.MoveTo(LerpOnCurve(points, distances, startDist));

            for (int i = 1; i < points.Length; i++)
            {
                if (distances[i] < startDist) continue;
                if (distances[i] > endDist)
                {
                    painter.LineTo(LerpOnCurve(points, distances, endDist));
                    break;
                }
                painter.LineTo(points[i]);
            }

            if (distances[points.Length - 1] <= endDist)
                painter.LineTo(LerpOnCurve(points, distances, endDist));

            painter.Stroke();
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

        private static Vector2 EvalBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
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
