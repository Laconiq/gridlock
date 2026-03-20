using Unity.Cinemachine;
using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Look Roll")]
    public class LookRollExtension : CinemachineExtension
    {
        [SerializeField] private float maxRollAngle = 2f;
        [SerializeField] private float rollSmoothTime = 0.1f;

        private float _currentRoll;
        private float _rollVelocity;
        private PlayerCamera _camera;

        protected override void Awake()
        {
            base.Awake();
            _camera = GetComponentInParent<PlayerCamera>();
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || _camera == null) return;

            float lookX = _camera.LookDelta.x;
            float targetRoll = -lookX * maxRollAngle * 0.1f;
            targetRoll = Mathf.Clamp(targetRoll, -maxRollAngle, maxRollAngle);

            _currentRoll = Mathf.SmoothDamp(_currentRoll, targetRoll, ref _rollVelocity, rollSmoothTime);

            state.Lens.Dutch += _currentRoll;
        }
    }
}
