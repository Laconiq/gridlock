using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gridlock.CameraSystem
{
    public class TopDownCamera : MonoBehaviour
    {
        public static TopDownCamera Instance { get; private set; }

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

        [Header("Focus")]
        [SerializeField] private float focusDuration = 0.35f;
        [SerializeField] private float restoreDuration = 0.3f;

        private Camera _camera;
        private Controls _controls;
        private Vector3 _camRight;
        private Vector3 _camUp;
        private bool _isPanning;
        private float _targetSize;

        private Vector3 _savedPosition;
        private float _savedOrthoSize;
        private bool _hasSavedState;
        private bool _isFocusing;
        private bool _inputEnabled = true;
        private Coroutine _focusCoroutine;

        private void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
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
            if (Instance == this) Instance = null;
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
            if (_isFocusing || !_inputEnabled) return;
            if (!_isPanning) return;

            var delta = _controls.Player.CameraDelta.ReadValue<Vector2>();
            if (delta.sqrMagnitude < 0.01f) return;

            var right = transform.right;
            var up = Vector3.Cross(right, Vector3.up).normalized;

            var move = (-delta.x * right - delta.y * up) * (dragSpeed * _camera.orthographicSize * Time.deltaTime);
            transform.position += move;
        }

        private void HandleZoom()
        {
            if (_isFocusing || !_inputEnabled) return;

            var scroll = _controls.Player.CameraZoom.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) < 0.01f) return;

            _targetSize -= scroll.y * zoomSpeed;
            _targetSize = Mathf.Clamp(_targetSize, minSize, maxSize);

            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetSize, zoomSmoothing * Time.deltaTime);
        }

        private void ClampPosition()
        {
            if (Mathf.Abs(_camera.orthographicSize - _targetSize) > 0.01f)
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, _targetSize, zoomSmoothing * Time.deltaTime);

            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
            pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
            transform.position = pos;
        }

        public void FocusOn(Vector3 worldPosition, float targetOrthoSize, Action onComplete = null)
        {
            _savedPosition = transform.position;
            _savedOrthoSize = _camera.orthographicSize;
            _hasSavedState = true;
            _isFocusing = true;

            if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);

            // For orthographic camera, compute position so worldPosition projects to screen center
            var forward = transform.forward;
            float t = (worldPosition.y - transform.position.y) / forward.y;
            var targetPos = worldPosition - forward * t;

            _focusCoroutine = StartCoroutine(LerpCamera(targetPos, targetOrthoSize, focusDuration, () =>
            {
                _isFocusing = false;
                onComplete?.Invoke();
            }));
        }

        public void RestoreFocus(Action onComplete = null)
        {
            if (!_hasSavedState)
            {
                onComplete?.Invoke();
                return;
            }

            _isFocusing = true;

            if (_focusCoroutine != null) StopCoroutine(_focusCoroutine);

            _focusCoroutine = StartCoroutine(LerpCamera(_savedPosition, _savedOrthoSize, restoreDuration, () =>
            {
                _hasSavedState = false;
                _isFocusing = false;
                onComplete?.Invoke();
            }));
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        private IEnumerator LerpCamera(Vector3 targetPos, float targetSize, float duration, Action onComplete)
        {
            var startPos = transform.position;
            var startSize = _camera.orthographicSize;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                var pos = Vector3.Lerp(startPos, targetPos, t);
                transform.position = pos;
                _camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

                yield return null;
            }

            transform.position = targetPos;
            _camera.orthographicSize = targetSize;
            _targetSize = targetSize;

            _focusCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
