using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/UI/Dynamic Crosshair")]
    [RequireComponent(typeof(UIDocument))]
    public class DynamicCrosshair : MonoBehaviour
    {
        [Header("Line Appearance")]
        [SerializeField] private float lineLength = 12f;
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private Color lineColor = Color.white;

        [Header("Spread Settings")]
        [SerializeField] private float baseSpread = 8f;
        [SerializeField] private float moveSpread = 20f;
        [SerializeField] private float airSpread = 30f;
        [SerializeField] private float spreadSmoothTime = 0.08f;

        private UIDocument _uiDocument;
        private VisualElement _canvas;
        private PlayerController _player;
        private float _currentSpread;
        private float _spreadVelocity;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _currentSpread = baseSpread;
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _canvas = root.Q("crosshair-canvas");
            if (_canvas != null)
                _canvas.generateVisualContent += OnGenerateVisualContent;
        }

        private void OnDisable()
        {
            if (_canvas != null)
                _canvas.generateVisualContent -= OnGenerateVisualContent;
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player == null) return;
            }

            float targetSpread = baseSpread;

            if (!_player.IsGrounded)
                targetSpread = airSpread;
            else if (_player.CurrentSpeedNormalized > 0.1f)
                targetSpread = Mathf.Lerp(baseSpread, moveSpread, _player.CurrentSpeedNormalized);

            float prev = _currentSpread;
            _currentSpread = Mathf.SmoothDamp(_currentSpread, targetSpread, ref _spreadVelocity, spreadSmoothTime);

            if (Mathf.Abs(_currentSpread - prev) > 0.01f)
                _canvas?.MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var rect = _canvas.contentRect;
            if (rect.width <= 0 || rect.height <= 0) return;

            var painter = mgc.painter2D;
            painter.strokeColor = lineColor;
            painter.lineWidth = lineThickness;
            painter.lineCap = LineCap.Butt;

            float cx = rect.width * 0.5f;
            float cy = rect.height * 0.5f;
            float spread = _currentSpread;
            float len = lineLength;

            painter.BeginPath();
            painter.MoveTo(new Vector2(cx, cy - spread));
            painter.LineTo(new Vector2(cx, cy - spread - len));
            painter.Stroke();

            painter.BeginPath();
            painter.MoveTo(new Vector2(cx, cy + spread));
            painter.LineTo(new Vector2(cx, cy + spread + len));
            painter.Stroke();

            painter.BeginPath();
            painter.MoveTo(new Vector2(cx - spread, cy));
            painter.LineTo(new Vector2(cx - spread - len, cy));
            painter.Stroke();

            painter.BeginPath();
            painter.MoveTo(new Vector2(cx + spread, cy));
            painter.LineTo(new Vector2(cx + spread + len, cy));
            painter.Stroke();
        }
    }
}
