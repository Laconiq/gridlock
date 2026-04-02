using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Camera
{
    public sealed class IsometricCamera
    {
        private const float PITCH_DEG = 30f;
        private const float YAW_DEG = 45f;
        private const float CAMERA_DISTANCE = 50f;

        public float DragSpeed { get; set; } = 0.04f;
        public float ZoomSpeed { get; set; } = 2f;
        public float MinSize { get; set; } = 8f;
        public float MaxSize { get; set; } = 22f;
        public float ZoomSmoothing { get; set; } = 8f;
        public Vector2 BoundsMin { get; set; } = new(-30f, -20f);
        public Vector2 BoundsMax { get; set; } = new(30f, 20f);

        private Vector3 _focusPoint;
        private float _orthoSize = 14f;
        private float _targetOrthoSize = 14f;
        private bool _isPanning;

        private Vector3 _shakeOffset;

        private readonly Vector3 _forward;
        private readonly Vector3 _right;
        private readonly Vector3 _up;

        public float OrthoSize => _orthoSize;
        public Vector3 FocusPoint => _focusPoint;

        public IsometricCamera()
        {
            float pitchRad = PITCH_DEG * MathF.PI / 180f;
            float yawRad = YAW_DEG * MathF.PI / 180f;

            _forward = new Vector3(
                MathF.Sin(yawRad) * MathF.Cos(pitchRad),
                -MathF.Sin(pitchRad),
                MathF.Cos(yawRad) * MathF.Cos(pitchRad)
            );
            _forward = Vector3.Normalize(_forward);

            _right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, _forward));
            // "Up" in the ground plane for panning (perpendicular to right, projected on XZ)
            _up = Vector3.Normalize(Vector3.Cross(_right, Vector3.UnitY));
        }

        public void Init()
        {
            _focusPoint = Vector3.Zero;
            _orthoSize = 14f;
            _targetOrthoSize = 14f;
        }

        public void LateUpdate(float dt)
        {
            HandlePan(dt);
            HandleZoom(dt);
            ClampPosition(dt);
        }

        private void HandlePan(float dt)
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Middle))
                _isPanning = true;
            if (Raylib.IsMouseButtonReleased(MouseButton.Middle))
                _isPanning = false;

            if (!_isPanning) return;

            var delta = Raylib.GetMouseDelta();
            if (delta.X * delta.X + delta.Y * delta.Y < 0.01f) return;

            var move = (-delta.X * _right - delta.Y * _up) * (DragSpeed * _orthoSize * dt);
            _focusPoint += move;
        }

        public bool ZoomEnabled { get; set; } = true;

        private void HandleZoom(float dt)
        {
            if (!ZoomEnabled) return;
            float scroll = Raylib.GetMouseWheelMove();
            if (MathF.Abs(scroll) < 0.01f) return;

            _targetOrthoSize -= scroll * ZoomSpeed;
            _targetOrthoSize = Math.Clamp(_targetOrthoSize, MinSize, MaxSize);
        }

        private void ClampPosition(float dt)
        {
            if (MathF.Abs(_orthoSize - _targetOrthoSize) > 0.01f)
            {
                float t = 1f - MathF.Exp(-ZoomSmoothing * dt);
                _orthoSize += (_targetOrthoSize - _orthoSize) * t;
            }
            else
            {
                _orthoSize = _targetOrthoSize;
            }

            _focusPoint = new Vector3(
                Math.Clamp(_focusPoint.X, BoundsMin.X, BoundsMax.X),
                _focusPoint.Y,
                Math.Clamp(_focusPoint.Z, BoundsMin.Y, BoundsMax.Y)
            );
        }

        public void SetShakeOffset(Vector3 offset)
        {
            _shakeOffset = offset;
        }

        /// <summary>
        /// Returns a Raylib Camera3D configured for orthographic isometric view.
        /// Call this each frame before BeginMode3D.
        /// </summary>
        public Camera3D Apply()
        {
            var cameraPos = _focusPoint - _forward * CAMERA_DISTANCE + _shakeOffset;

            var rlCam = new Camera3D
            {
                Position = ToRlVec3(cameraPos),
                Target = ToRlVec3(_focusPoint + _shakeOffset),
                Up = new System.Numerics.Vector3(0f, 1f, 0f),
                FovY = _orthoSize * 2f,
                Projection = CameraProjection.Orthographic
            };

            return rlCam;
        }

        /// <summary>
        /// Gets a ray from screen position into the world, useful for mouse picking.
        /// </summary>
        public Ray GetScreenToWorldRay(System.Numerics.Vector2 screenPos)
        {
            var cam = Apply();
            return Raylib.GetScreenToWorldRay(screenPos, cam);
        }

        /// <summary>
        /// Intersects a screen-space ray with the Y=0 ground plane. Returns the hit point.
        /// </summary>
        public bool ScreenToGroundPoint(System.Numerics.Vector2 screenPos, out Vector3 groundPoint)
        {
            var ray = GetScreenToWorldRay(screenPos);
            var rayPos = new Vector3(ray.Position.X, ray.Position.Y, ray.Position.Z);
            var rayDir = new Vector3(ray.Direction.X, ray.Direction.Y, ray.Direction.Z);

            if (MathF.Abs(rayDir.Y) < 0.0001f)
            {
                groundPoint = Vector3.Zero;
                return false;
            }

            float t = -rayPos.Y / rayDir.Y;
            if (t < 0f)
            {
                groundPoint = Vector3.Zero;
                return false;
            }

            groundPoint = rayPos + rayDir * t;
            return true;
        }

        private static System.Numerics.Vector3 ToRlVec3(Vector3 v) => new(v.X, v.Y, v.Z);
    }
}
