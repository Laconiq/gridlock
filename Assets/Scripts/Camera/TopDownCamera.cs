using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.CameraSystem
{
    public class TopDownCamera : MonoBehaviour
    {
        [Header("Pan")]
        [SerializeField] private float dragSpeed = 0.04f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minSize = 8f;
        [SerializeField] private float maxSize = 22f;
        [SerializeField] private float zoomSmoothing = 8f;

        [Header("Bounds")]
        [SerializeField] private Vector2 boundsMin = new(-30f, -20f);
        [SerializeField] private Vector2 boundsMax = new(30f, 20f);

        private Camera _camera;
        private Controls _controls;
        private Quaternion _yawRotation;
        private bool _isPanning;
        private float _targetSize;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _yawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            _controls = new Controls();
        }

        private void OnEnable()
        {
            _controls.Player.Enable();

            _controls.Player.CameraPan.started += OnPanStarted;
            _controls.Player.CameraPan.canceled += OnPanCanceled;

            _targetSize = _camera.orthographicSize;
        }

        private void OnDisable()
        {
            _controls.Player.CameraPan.started -= OnPanStarted;
            _controls.Player.CameraPan.canceled -= OnPanCanceled;

            _controls.Player.Disable();
        }

        private void OnDestroy()
        {
            _controls?.Dispose();
        }

        private void OnPanStarted(InputAction.CallbackContext ctx) => _isPanning = true;
        private void OnPanCanceled(InputAction.CallbackContext ctx) => _isPanning = false;

        private void LateUpdate()
        {
            HandlePan();
            HandleZoom();
            ClampPosition();
        }

        private void HandlePan()
        {
            if (!_isPanning) return;

            var delta = _controls.Player.CameraDelta.ReadValue<Vector2>();
            if (delta.sqrMagnitude < 0.01f) return;

            var move = _yawRotation * new Vector3(-delta.x, 0f, -delta.y) * (dragSpeed * _camera.orthographicSize * Time.deltaTime);
            transform.position += move;
        }

        private void HandleZoom()
        {
            var scroll = _controls.Player.CameraZoom.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < 0.01f) return;

            _targetSize -= scroll.y * zoomSpeed;
            _targetSize = Mathf.Clamp(_targetSize, minSize, maxSize);

            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetSize, zoomSmoothing * Time.deltaTime);
        }

        private void ClampPosition()
        {
            // Smooth zoom lerp even when not scrolling
            if (Mathf.Abs(_camera.orthographicSize - _targetSize) > 0.01f)
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetSize, zoomSmoothing * Time.deltaTime);

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
            pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
            transform.position = pos;
        }
    }
}
