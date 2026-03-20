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

        [Header("Settings")]
        [SerializeField] private float baseSpread = 8f;
        [SerializeField] private float moveSpread = 20f;
        [SerializeField] private float airSpread = 30f;
        [SerializeField] private float spreadSmoothTime = 0.08f;

        private PlayerController _player;
        private float _currentSpread;
        private float _spreadVelocity;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (_player == null) return;

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
