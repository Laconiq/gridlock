using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Weapon ViewModel")]
    public class WeaponViewModel : MonoBehaviour
    {
        [Header("Sway (mouse-driven rotation)")]
        [SerializeField] private float swayAmount = 2f;
        [SerializeField] private float maxSway = 4f;
        [SerializeField] private float swaySmoothTime = 0.08f;

        [Header("Position Lag (spring follow)")]
        [SerializeField] private float lagAmount = 0.002f;
        [SerializeField] private float maxLag = 0.015f;
        [SerializeField] private float lagSmoothTime = 0.06f;

        [Header("Walk Bob (procedural figure-8)")]
        [SerializeField] private float bobFrequencyX = 0.6f;
        [SerializeField] private float bobFrequencyY = 1.2f;
        [SerializeField] private float bobAmplitudeX = 0.003f;
        [SerializeField] private float bobAmplitudeY = 0.004f;
        [SerializeField] private float bobSmooth = 8f;

        private PlayerCamera _camera;
        private PlayerController _player;
        private Vector3 _swayVelocity;
        private Vector3 _currentSwayRotation;
        private Vector3 _lagVelocity;
        private Vector3 _currentLagOffset;
        private Vector3 _bobVelocity;
        private Vector3 _currentBobOffset;
        private Vector3 _baseLocalPosition;
        private float _bobTimer;

        private void Awake()
        {
            _camera = GetComponentInParent<PlayerCamera>();
            _player = GetComponentInParent<PlayerController>();
            _baseLocalPosition = transform.localPosition;
        }

        private void LateUpdate()
        {
            if (_camera == null || _player == null) return;

            Vector2 lookDelta = _camera.LookDelta;

            HandleSway(lookDelta);
            HandleLag(lookDelta);
            HandleBob();

            transform.localPosition = _baseLocalPosition + _currentLagOffset + _currentBobOffset;
            transform.localRotation = Quaternion.Euler(_currentSwayRotation);
        }

        private void HandleSway(Vector2 lookDelta)
        {
            float swayX = Mathf.Clamp(-lookDelta.y * swayAmount, -maxSway, maxSway);
            float swayY = Mathf.Clamp(-lookDelta.x * swayAmount, -maxSway, maxSway);
            var target = new Vector3(swayX, swayY, 0f);

            _currentSwayRotation = Vector3.SmoothDamp(
                _currentSwayRotation, target, ref _swayVelocity, swaySmoothTime);
        }

        private void HandleLag(Vector2 lookDelta)
        {
            float lagX = Mathf.Clamp(-lookDelta.x * lagAmount, -maxLag, maxLag);
            float lagY = Mathf.Clamp(-lookDelta.y * lagAmount, -maxLag, maxLag);
            var target = new Vector3(lagX, lagY, 0f);

            _currentLagOffset = Vector3.SmoothDamp(
                _currentLagOffset, target, ref _lagVelocity, lagSmoothTime);
        }

        private void HandleBob()
        {
            float speed = _player.CurrentSpeedNormalized;
            bool isMoving = speed > 0.1f && _player.IsGrounded;

            Vector3 targetBob;
            if (isMoving)
            {
                _bobTimer += Time.deltaTime * speed;
                targetBob = new Vector3(
                    Mathf.Sin(_bobTimer * bobFrequencyX * Mathf.PI * 2f) * bobAmplitudeX * speed,
                    Mathf.Sin(_bobTimer * bobFrequencyY * Mathf.PI * 2f) * bobAmplitudeY * speed,
                    0f
                );
            }
            else
            {
                targetBob = Vector3.zero;
            }

            _currentBobOffset = Vector3.SmoothDamp(
                _currentBobOffset, targetBob, ref _bobVelocity, 1f / bobSmooth);
        }
    }
}
