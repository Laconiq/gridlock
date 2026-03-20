using UnityEngine;
using UnityEngine.UI;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/UI/Dynamic Crosshair")]
    public class DynamicCrosshair : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform top;
        [SerializeField] private RectTransform bottom;
        [SerializeField] private RectTransform left;
        [SerializeField] private RectTransform right;

        [Header("Line Appearance")]
        [SerializeField] private float lineLength = 12f;
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private Color lineColor = Color.white;

        [Header("Spread Settings")]
        [SerializeField] private float baseSpread = 8f;
        [SerializeField] private float moveSpread = 20f;
        [SerializeField] private float airSpread = 30f;
        [SerializeField] private float spreadSmoothTime = 0.08f;

        private PlayerController _player;
        private float _currentSpread;
        private float _spreadVelocity;

        private void Awake()
        {
            _player = FindAnyObjectByType<PlayerController>();

            if (top == null || bottom == null || left == null || right == null)
                CreateCrosshairLines();

            _currentSpread = baseSpread;
        }

        private void CreateCrosshairLines()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            foreach (Transform child in transform)
                Destroy(child.gameObject);

            top = CreateLine("Top", rt);
            bottom = CreateLine("Bottom", rt);
            left = CreateLine("Left", rt);
            right = CreateLine("Right", rt);

            SetLineSize(top, lineThickness, lineLength);
            SetLineSize(bottom, lineThickness, lineLength);
            SetLineSize(left, lineLength, lineThickness);
            SetLineSize(right, lineLength, lineThickness);
        }

        private RectTransform CreateLine(string lineName, RectTransform parent)
        {
            var go = new GameObject(lineName, typeof(RectTransform), typeof(Image));
            var lineRt = go.GetComponent<RectTransform>();
            lineRt.SetParent(parent, false);
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.pivot = new Vector2(0.5f, 0.5f);

            var img = go.GetComponent<Image>();
            img.color = lineColor;
            img.raycastTarget = false;

            return lineRt;
        }

        private void SetLineSize(RectTransform rt, float width, float height)
        {
            rt.sizeDelta = new Vector2(width, height);
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

            _currentSpread = Mathf.SmoothDamp(_currentSpread, targetSpread, ref _spreadVelocity, spreadSmoothTime);

            if (top != null) top.anchoredPosition = new Vector2(0, _currentSpread);
            if (bottom != null) bottom.anchoredPosition = new Vector2(0, -_currentSpread);
            if (left != null) left.anchoredPosition = new Vector2(-_currentSpread, 0);
            if (right != null) right.anchoredPosition = new Vector2(_currentSpread, 0);
        }
    }
}
