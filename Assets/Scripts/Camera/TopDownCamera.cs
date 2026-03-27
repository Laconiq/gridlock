using UnityEngine;

namespace AIWE.CameraSystem
{
    public class TopDownCamera : MonoBehaviour
    {
        [Header("Position")]
        [SerializeField] private float height = 30f;
        [SerializeField] private float angle = 60f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float edgePanMargin = 20f;
        [SerializeField] private bool enableEdgePan = true;
        [SerializeField] private bool enableMiddleMouseDrag = true;
        [SerializeField] private float dragSpeed = 0.5f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minHeight = 10f;
        [SerializeField] private float maxHeight = 50f;

        [Header("Bounds")]
        [SerializeField] private Vector2 boundsMin = new(-50f, -50f);
        [SerializeField] private Vector2 boundsMax = new(50f, 50f);

        private Camera _camera;
        private Vector3 _dragOrigin;
        private bool _isDragging;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        private void Start()
        {
            ApplyAngle();
        }

        private void ApplyAngle()
        {
            transform.rotation = Quaternion.Euler(angle, 0f, 0f);
            var pos = transform.position;
            pos.y = height;
            transform.position = pos;
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
            var input = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) input.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) input.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) input.x += 1f;

            if (input.sqrMagnitude > 0f)
            {
                input.Normalize();
                var move = input * (panSpeed * Time.deltaTime);
                transform.position += move;
            }
        }

        private void HandleEdgePan()
        {
            if (!enableEdgePan) return;

            var mousePos = Input.mousePosition;
            var input = Vector3.zero;

            if (mousePos.x < edgePanMargin) input.x -= 1f;
            if (mousePos.x > Screen.width - edgePanMargin) input.x += 1f;
            if (mousePos.y < edgePanMargin) input.z -= 1f;
            if (mousePos.y > Screen.height - edgePanMargin) input.z += 1f;

            if (input.sqrMagnitude > 0f)
            {
                input.Normalize();
                transform.position += input * (panSpeed * Time.deltaTime);
            }
        }

        private void HandleMiddleMouseDrag()
        {
            if (!enableMiddleMouseDrag) return;

            if (Input.GetMouseButtonDown(2))
            {
                _isDragging = true;
                _dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2))
                _isDragging = false;

            if (_isDragging)
            {
                var delta = (Vector3)Input.mousePosition - _dragOrigin;
                _dragOrigin = Input.mousePosition;

                var move = new Vector3(-delta.x, 0f, -delta.y) * (dragSpeed * Time.deltaTime);
                transform.position += move;
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            height -= scroll * zoomSpeed;
            height = Mathf.Clamp(height, minHeight, maxHeight);

            var pos = transform.position;
            pos.y = height;
            transform.position = pos;
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
