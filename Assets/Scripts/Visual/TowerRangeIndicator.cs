using Gridlock.Grid;
using Gridlock.Towers;
using UnityEngine;

namespace Gridlock.Visual
{
    [RequireComponent(typeof(TowerChassis))]
    public class TowerRangeIndicator : MonoBehaviour
    {
        [SerializeField] private Material lineMaterial;
        [SerializeField] private int segments = 48;
        [SerializeField] private float lineWidth = 0.06f;
        [SerializeField] private float lineY = 0.12f;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMin = 0.3f;
        [SerializeField] private float pulseMax = 0.7f;

        private LineRenderer _line;
        private TowerChassis _chassis;
        private Vector3[] _basePositions;
        private bool _visible;
        private Color _baseColor = new(0f, 0.87f, 0.93f); // PrimaryDim cyan

        private void Awake()
        {
            _chassis = GetComponent<TowerChassis>();
            CreateCircle();
            Hide();
        }

        private void CreateCircle()
        {
            var go = new GameObject("RangeIndicator");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            _line = go.AddComponent<LineRenderer>();
            _line.material = lineMaterial;
            _line.startWidth = lineWidth;
            _line.endWidth = lineWidth;
            _line.positionCount = segments + 1;
            _line.useWorldSpace = true;
            _line.loop = false;
            _line.numCornerVertices = 0;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            RebuildPositions();
        }

        private void RebuildPositions()
        {
            float range = _chassis != null ? _chassis.BaseRange : 10f;
            var center = transform.position;
            _basePositions = new Vector3[segments + 1];

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * range;
                float z = center.z + Mathf.Sin(angle) * range;
                _basePositions[i] = new Vector3(x, lineY, z);
                _line.SetPosition(i, _basePositions[i]);
            }
        }

        public void Show()
        {
            if (_visible) return;
            _visible = true;
            RebuildPositions();
            _line.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            _line.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_visible) return;

            float alpha = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            var color = _baseColor;
            color.a = alpha;
            _line.startColor = color;
            _line.endColor = color;

            var warp = GridWarpManager.Instance;
            if (warp == null) return;

            for (int i = 0; i <= segments; i++)
            {
                var basePos = _basePositions[i];
                float offset = warp.GetWarpOffset(basePos.x, basePos.z);
                _line.SetPosition(i, new Vector3(basePos.x, lineY + offset, basePos.z));
            }
        }
    }
}
