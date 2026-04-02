using System;
using System.Numerics;
using Raylib_cs;

namespace Gridlock.Grid
{
    public sealed class GridWarpManager
    {
        public static GridWarpManager? Instance { get; private set; }

        private float _anchorStiffness = 4.0f;
        private float _neighborStiffness = 60f;
        private float _damping = 2.5f;
        private float _tintDiffusion = 12f;
        private float _tintDecay = 0.15f;
        private float _glowFromDisplacement = 8f;
        private float _glowFromVelocity = 2f;

        private Vector3[] _positions = Array.Empty<Vector3>();
        private Vector3[] _restPositions = Array.Empty<Vector3>();
        private Vector3[] _velocities = Array.Empty<Vector3>();
        private Vector4[] _tints = Array.Empty<Vector4>();
        private Vector4[] _tintBuffer = Array.Empty<Vector4>();
        private Vector4[] _colors = Array.Empty<Vector4>();

        private int _resX, _resZ;
        private int _vertCountX;
        private float _width, _height;
        private bool _initialized;

        public int VertexCount => _positions.Length;
        public Vector3[] Positions => _positions;
        public Vector4[] Colors => _colors;
        public bool Initialized => _initialized;

        public void Init(int resX, int resZ, float width, float height, Vector3[] restPositions)
        {
            Instance = this;
            _resX = resX;
            _resZ = resZ;
            _vertCountX = resX + 1;
            _width = width;
            _height = height;

            int count = restPositions.Length;
            _restPositions = restPositions;
            _positions = new Vector3[count];
            Array.Copy(_restPositions, _positions, count);
            _velocities = new Vector3[count];
            _tints = new Vector4[count];
            _tintBuffer = new Vector4[count];
            _colors = new Vector4[count];
            _initialized = true;
        }

        public void Shutdown()
        {
            if (Instance == this) Instance = null;
            _initialized = false;
        }

        public float GetWarpOffset(float worldX, float worldZ)
        {
            if (!_initialized) return 0f;

            float lx = (worldX + _width * 0.5f) / _width * _resX;
            float lz = (worldZ + _height * 0.5f) / _height * _resZ;

            int x0 = Math.Clamp((int)MathF.Floor(lx), 0, _resX - 1);
            int z0 = Math.Clamp((int)MathF.Floor(lz), 0, _resZ - 1);

            float tx = lx - x0;
            float tz = lz - z0;

            float y00 = _positions[z0 * _vertCountX + x0].Y;
            float y10 = _positions[z0 * _vertCountX + x0 + 1].Y;
            float y01 = _positions[(z0 + 1) * _vertCountX + x0].Y;
            float y11 = _positions[(z0 + 1) * _vertCountX + x0 + 1].Y;

            float a = y00 + (y10 - y00) * tx;
            float b = y01 + (y11 - y01) * tx;
            return a + (b - a) * tz;
        }

        public void DropStone(Vector3 center, float force, float splashRadius, Color color)
        {
            if (!_initialized) return;

            var tintColor = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            float radiusSq = splashRadius * splashRadius;

            for (int i = 0; i < _positions.Length; i++)
            {
                float dx = _positions[i].X - center.X;
                float dz = _positions[i].Z - center.Z;
                float distSq = dx * dx + dz * dz;
                if (distSq > radiusSq) continue;

                float t = MathF.Sqrt(distSq) / splashRadius;
                float falloff = MathF.Exp(-t * t * 4f);

                _velocities[i].Y -= force * falloff;
                _tints[i] = Vector4.Lerp(_tints[i], tintColor, falloff);
            }
        }

        public void Shockwave(Vector3 center, float force, float splashRadius, Color color)
        {
            if (!_initialized) return;

            var tintColor = new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
            float radiusSq = splashRadius * splashRadius;

            for (int i = 0; i < _positions.Length; i++)
            {
                float dx = _positions[i].X - center.X;
                float dz = _positions[i].Z - center.Z;
                float distSq = dx * dx + dz * dz;
                if (distSq > radiusSq || distSq < 0.01f) continue;

                float dist = MathF.Sqrt(distSq);
                float t = dist / splashRadius;
                float falloff = MathF.Exp(-t * t * 4f);

                _velocities[i].X += dx / dist * force * falloff * 0.3f;
                _velocities[i].Z += dz / dist * force * falloff * 0.3f;
                _velocities[i].Y -= force * falloff;

                _tints[i] = Vector4.Lerp(_tints[i], tintColor, falloff);
            }
        }

        public void Update(float dt)
        {
            if (!_initialized) return;
            if (dt <= 0f || dt > 0.1f) return;

            SimulateSprings(dt);
            DiffuseTints(dt);
            ComputeVertexColors();
        }

        private void SimulateSprings(float dt)
        {
            int count = _positions.Length;
            int vcx = _vertCountX;

            for (int i = 0; i < count; i++)
            {
                var force = (_restPositions[i] - _positions[i]) * _anchorStiffness;

                int x = i % vcx;
                int z = i / vcx;

                if (x > 0) force += SpringForce(i, i - 1);
                if (x < _resX) force += SpringForce(i, i + 1);
                if (z > 0) force += SpringForce(i, i - vcx);
                if (z < _resZ) force += SpringForce(i, i + vcx);

                force -= _velocities[i] * _damping;

                _velocities[i] += force * dt;
                _positions[i] += _velocities[i] * dt;
            }
        }

        private Vector3 SpringForce(int from, int to)
        {
            var delta = _positions[to] - _positions[from];
            float dist = delta.Length();
            if (dist < 0.001f) return Vector3.Zero;

            float restDist = (_restPositions[to] - _restPositions[from]).Length();
            return delta / dist * ((dist - restDist) * _neighborStiffness);
        }

        private void DiffuseTints(float dt)
        {
            int count = _tints.Length;
            int vcx = _vertCountX;
            float diffuse = _tintDiffusion * dt;
            float decay = _tintDecay * dt;

            Array.Copy(_tints, _tintBuffer, count);

            for (int i = 0; i < count; i++)
            {
                int x = i % vcx;
                int z = i / vcx;
                int neighbors = 0;
                var sum = Vector4.Zero;

                if (x > 0) { sum += _tintBuffer[i - 1]; neighbors++; }
                if (x < _resX) { sum += _tintBuffer[i + 1]; neighbors++; }
                if (z > 0) { sum += _tintBuffer[i - vcx]; neighbors++; }
                if (z < _resZ) { sum += _tintBuffer[i + vcx]; neighbors++; }

                var avg = sum / neighbors;
                _tints[i] = Vector4.Lerp(_tints[i], avg, diffuse);
                _tints[i] = Vector4.Lerp(_tints[i], Vector4.Zero, decay);
            }
        }

        private void ComputeVertexColors()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                float disp = MathF.Abs(_positions[i].Y - _restPositions[i].Y);
                float speed = _velocities[i].Length();
                float energy = Math.Clamp(disp * _glowFromDisplacement + speed * _glowFromVelocity, 0f, 1f);

                _colors[i] = new Vector4(
                    _tints[i].X * energy,
                    _tints[i].Y * energy,
                    _tints[i].Z * energy,
                    energy
                );
            }
        }
    }
}
