using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    public class PlayerCamera : NetworkBehaviour
    {
        [Header("Look Settings")]
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("References")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private CinemachinePanTilt panTilt;

        private Controls _controls;
        private float _yaw;
        private float _pitch;

        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            _controls = new Controls();

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

            var lookInput = _controls.Player.Look.ReadValue<Vector2>();

            _yaw += lookInput.x * sensitivity;
            _pitch -= lookInput.y * sensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            // Rotate the player body horizontally
            transform.parent.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Update Cinemachine PanTilt for vertical look
            if (panTilt != null)
            {
                panTilt.PanAxis.Value = 0f;
                panTilt.TiltAxis.Value = _pitch;
            }
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
