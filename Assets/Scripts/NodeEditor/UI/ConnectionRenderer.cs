using UnityEngine;
using UnityEngine.UI;

namespace AIWE.NodeEditor.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class ConnectionRenderer : MaskableGraphic
    {
        [SerializeField] private float thickness = 3f;
        [SerializeField] private int segments = 20;

        private RectTransform _startPoint;
        private RectTransform _endPoint;

        public void SetEndpoints(RectTransform start, RectTransform end, Color? lineColor = null)
        {
            _startPoint = start;
            _endPoint = end;
            if (lineColor.HasValue)
                color = lineColor.Value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_startPoint == null || _endPoint == null) return;

            var startPos = GetLocalPoint(_startPoint);
            var endPos = GetLocalPoint(_endPoint);

            var controlOffset = Mathf.Abs(endPos.x - startPos.x) * 0.5f;
            controlOffset = Mathf.Max(controlOffset, 50f);

            var cp1 = startPos + Vector2.right * controlOffset;
            var cp2 = endPos + Vector2.left * controlOffset;

            Vector2 prevPoint = startPos;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                var point = CubicBezier(startPos, cp1, cp2, endPos, t);
                AddLineSegment(vh, prevPoint, point);
                prevPoint = point;
            }
        }

        private void AddLineSegment(VertexHelper vh, Vector2 a, Vector2 b)
        {
            var dir = (b - a).normalized;
            var normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;

            int idx = vh.currentVertCount;

            vh.AddVert(a + normal, color, Vector4.zero);
            vh.AddVert(a - normal, color, Vector4.zero);
            vh.AddVert(b - normal, color, Vector4.zero);
            vh.AddVert(b + normal, color, Vector4.zero);

            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 2, idx + 3);
        }

        private Vector2 GetLocalPoint(RectTransform target)
        {
            if (target == null) return Vector2.zero;

            var worldPos = target.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, RectTransformUtility.WorldToScreenPoint(null, worldPos),
                null, out var localPoint);
            return localPoint;
        }

        private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        private void Update()
        {
            if (_startPoint != null && _endPoint != null)
            {
                SetVerticesDirty();
            }
        }
    }
}
