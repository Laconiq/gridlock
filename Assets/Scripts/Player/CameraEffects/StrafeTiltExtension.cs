using Unity.Cinemachine;
using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Strafe Tilt")]
    public class StrafeTiltExtension : CinemachineExtension
    {
        [SerializeField] private float maxTiltAngle = 3.5f;
        [SerializeField] private float tiltSpeed = 8f;
        [SerializeField] private float returnSpeed = 12f;

        private float _currentTilt;
        private float _tiltVelocity;
        private PlayerController _player;

        protected override void Awake()
        {
            base.Awake();
            _player = GetComponentInParent<PlayerController>();
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || _player == null) return;

            float horizontalInput = _player.MoveInput.x;
            float targetTilt = -horizontalInput * maxTiltAngle;

            float smoothTime = Mathf.Abs(targetTilt) > Mathf.Abs(_currentTilt)
                ? 1f / tiltSpeed
                : 1f / returnSpeed;
            _currentTilt = Mathf.SmoothDamp(_currentTilt, targetTilt, ref _tiltVelocity, smoothTime);

            state.Lens.Dutch += _currentTilt;
        }
    }
}
