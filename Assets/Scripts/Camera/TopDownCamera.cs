using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.CameraSystem
{
    public class TopDownCamera : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minSize = 8f;
        [SerializeField] private float maxSize = 22f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float edgePanMargin = 20f;
        [SerializeField] private bool enableEdgePan = true;
        [SerializeField] private bool enableMiddleMouseDrag = true;
        [SerializeField] private float dragSpeed = 0.05f;

        [Header("Bounds")]
        [SerializeField] private Vector2 boundsMin = new(-30f, -20f);
        [SerializeField] private Vector2 boundsMax = new(30f, 20f);

        private Camera _camera;
        private Vector3 _dragOrigin;
        private bool _isDragging;
        private Quaternion _yawRotation;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _yawRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        private void LateUpdate()
        {
            HandleKeyboardPan();
            HandleEdgePan();
            HandleMiddleMouseDrag();
            HandleZoom();
            ClampPosition();
        }

        private void HandleKeyboardPan()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            var input = Vector3.zero;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.z -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;

            if (input.sqrMagnitude > 0f)
            {
                input.Normalize();
                transform.position += _yawRotation * input * (panSpeed * Time.deltaTime);
            }
        }

        private void HandleEdgePan()
        {
            if (!enableEdgePan) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            var mousePos = mouse.position.ReadValue();
            var input = Vector3.zero;

            if (mousePos.x < edgePanMargin) input.x -= 1f;
            if (mousePos.x > Screen.width - edgePanMargin) input.x += 1f;
            if (mousePos.y < edgePanMargin) input.z -= 1f;
            if (mousePos.y > Screen.height - edgePanMargin) input.z += 1f;

            if (input.sqrMagnitude > 0f)
            {
                input.Normalize();
                transform.position += _yawRotation * input * (panSpeed * Time.deltaTime);
            }
        }

        private void HandleMiddleMouseDrag()
        {
            if (!enableMiddleMouseDrag) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.middleButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _dragOrigin = (Vector3)mouse.position.ReadValue();
            }

            if (mouse.middleButton.wasReleasedThisFrame)
                _isDragging = false;

            if (_isDragging)
            {
                var current = (Vector3)mouse.position.ReadValue();
                var delta = current - _dragOrigin;
                _dragOrigin = current;

                var move = _yawRotation * new Vector3(-delta.x, 0f, -delta.y) * (dragSpeed * _camera.orthographicSize);
                transform.position += move;
            }
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            _camera.orthographicSize -= scroll * zoomSpeed * 0.01f;
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minSize, maxSize);
        }

        private void ClampPosition()
        {
            var pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
            pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
            transform.position = pos;
        }
    }
}
