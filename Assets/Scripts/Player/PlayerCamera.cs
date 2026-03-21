using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerCamera : NetworkBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Dynamic FOV")]
        [SerializeField] private float baseFov = 100f;
        [SerializeField] private float maxFovBonus = 10f;
        [SerializeField] private float fovSmoothTime = 0.2f;
        [SerializeField] private float fovSpeedThreshold = 0.5f;

        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachinePanTilt panTilt;

        private Controls _controls;
        private float _yaw;
        private float _pitch;
        private float _fovVelocity;
        private PlayerController _player;

        public bool InputEnabled { get; set; }
        public Vector2 LookDelta { get; private set; }

        private void Awake()
        {
            _controls = new Controls();
            _player = GetComponentInParent<PlayerController>();

            if (cinemachineCamera == null)
                cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
            if (panTilt == null)
                panTilt = GetComponentInChildren<CinemachinePanTilt>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                if (cinemachineCamera != null)
                    cinemachineCamera.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            _controls.Player.Enable();
            cinemachineCamera.Priority.Value = 100;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _controls.Player.Disable();
        }

        private void LateUpdate()
        {
            if (!IsOwner || !InputEnabled) return;

            HandleLook();
            HandleDynamicFov();
        }

        private float _recoilPitch;
        private float _recoilYaw;

        public void ApplyRecoil(float pitch, float yaw)
        {
            _recoilPitch = pitch;
            _recoilYaw = yaw;
        }

        private void HandleLook()
        {
            var lookInput = _controls.Player.Look.ReadValue<Vector2>();
            LookDelta = lookInput;

            _yaw += lookInput.x * sensitivity + _recoilYaw;
            _pitch -= lookInput.y * sensitivity;
            _pitch = Mathf.Clamp(_pitch + _recoilPitch, minPitch, maxPitch);

            transform.parent.rotation = Quaternion.Euler(0f, _yaw, 0f);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            if (panTilt != null)
            {
                panTilt.PanAxis.Value = 0f;
                panTilt.TiltAxis.Value = _pitch;
            }

            _recoilPitch = 0f;
            _recoilYaw = 0f;
        }

        private void HandleDynamicFov()
        {
            if (cinemachineCamera == null || _player == null) return;

            float speedNorm = _player.CurrentSpeedNormalized;
            float fovBonus = speedNorm > fovSpeedThreshold
                ? maxFovBonus * Mathf.InverseLerp(fovSpeedThreshold, 1f, speedNorm)
                : 0f;

            float targetFov = baseFov + fovBonus;
            var lens = cinemachineCamera.Lens;
            lens.FieldOfView = Mathf.SmoothDamp(lens.FieldOfView, targetFov, ref _fovVelocity, fovSmoothTime);
            cinemachineCamera.Lens = lens;
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
