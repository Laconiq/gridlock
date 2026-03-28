using UnityEngine;

namespace AIWE.Grid
{
    [DefaultExecutionOrder(150)]
    public class GridWarpManager : MonoBehaviour
    {
        public static GridWarpManager Instance { get; private set; }

        [Header("Spring Physics")]
        [SerializeField] private float anchorStiffness = 1.2f;
        [SerializeField] private float neighborStiffness = 20f;
        [SerializeField] private float damping = 0.8f;

        [Header("Color")]
        [SerializeField] private float tintDiffusion = 12f;
        [SerializeField] private float tintDecay = 0.15f;
        [SerializeField] private float glowFromDisplacement = 8f;
        [SerializeField] private float glowFromVelocity = 2f;

        private Mesh _mesh;
        private Vector3[] _positions;
        private Vector3[] _restPositions;
        private Vector3[] _velocities;
        private Color[] _tints;
        private Color[] _colors;
        private Color[] _tintBuffer;
        private int _resX, _resZ;
        private int _vertCountX;
        private float _width, _height;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(Mesh mesh, int resX, int resZ, float width, float height)
        {
            _mesh = mesh;
            _resX = resX;
            _resZ = resZ;
            _vertCountX = resX + 1;
            _width = width;
            _height = height;

            int count = mesh.vertexCount;
            _restPositions = mesh.vertices;
            _positions = (Vector3[])_restPositions.Clone();
            _velocities = new Vector3[count];
            _tints = new Color[count];
            _tintBuffer = new Color[count];
            _colors = new Color[count];
        }

        public float GetWarpOffset(float worldX, float worldZ)
        {
            if (_positions == null) return 0f;

            float lx = (worldX + _width * 0.5f) / _width * _resX;
            float lz = (worldZ + _height * 0.5f) / _height * _resZ;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(lx), 0, _resX - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(lz), 0, _resZ - 1);

            float tx = lx - x0;
            float tz = lz - z0;

            float y00 = _positions[z0 * _vertCountX + x0].y;
            float y10 = _positions[z0 * _vertCountX + x0 + 1].y;
            float y01 = _positions[(z0 + 1) * _vertCountX + x0].y;
            float y11 = _positions[(z0 + 1) * _vertCountX + x0 + 1].y;

            return Mathf.Lerp(Mathf.Lerp(y00, y10, tx), Mathf.Lerp(y01, y11, tx), tz);
        }

        public void DropStone(Vector3 center, float force, float splashRadius, Color color)
        {
            if (_positions == null) return;

            for (int i = 0; i < _positions.Length; i++)
            {
                float dx = _positions[i].x - center.x;
                float dz = _positions[i].z - center.z;
                float distSq = dx * dx + dz * dz;
                if (distSq > splashRadius * splashRadius) continue;

                float t = Mathf.Sqrt(distSq) / splashRadius;
                float falloff = Mathf.Exp(-t * t * 4f);

                _velocities[i].y -= force * falloff;
                _tints[i] = Color.Lerp(_tints[i], color, falloff);
            }
        }

        public void Shockwave(Vector3 center, float force, float splashRadius, Color color)
        {
            if (_positions == null) return;

            for (int i = 0; i < _positions.Length; i++)
            {
                float dx = _positions[i].x - center.x;
                float dz = _positions[i].z - center.z;
                float distSq = dx * dx + dz * dz;
                if (distSq > splashRadius * splashRadius || distSq < 0.01f) continue;

                float dist = Mathf.Sqrt(distSq);
                float t = dist / splashRadius;
                float falloff = Mathf.Exp(-t * t * 4f);

                _velocities[i].x += dx / dist * force * falloff * 0.3f;
                _velocities[i].z += dz / dist * force * falloff * 0.3f;
                _velocities[i].y -= force * falloff;

                _tints[i] = Color.Lerp(_tints[i], color, falloff);
            }
        }

        private void LateUpdate()
        {
            if (_positions == null || _mesh == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f || dt > 0.1f) return;

            SimulateSprings(dt);
            DiffuseTints(dt);
            ComputeVertexColors();
            UpdateMesh();
        }

        private void SimulateSprings(float dt)
        {
            int count = _positions.Length;
            int vcx = _vertCountX;

            for (int i = 0; i < count; i++)
            {
                var force = (_restPositions[i] - _positions[i]) * anchorStiffness;

                int x = i % vcx;
                int z = i / vcx;

                if (x > 0) force += SpringForce(i, i - 1);
                if (x < _resX) force += SpringForce(i, i + 1);
                if (z > 0) force += SpringForce(i, i - vcx);
                if (z < _resZ) force += SpringForce(i, i + vcx);

                force -= _velocities[i] * damping;

                _velocities[i] += force * dt;
                _positions[i] += _velocities[i] * dt;
            }
        }

        private Vector3 SpringForce(int from, int to)
        {
            var delta = _positions[to] - _positions[from];
            float dist = delta.magnitude;
            if (dist < 0.001f) return Vector3.zero;

            float restDist = (_restPositions[to] - _restPositions[from]).magnitude;
            return delta / dist * ((dist - restDist) * neighborStiffness);
        }

        private void DiffuseTints(float dt)
        {
            int count = _tints.Length;
            int vcx = _vertCountX;
            float diffuse = tintDiffusion * dt;
            float decay = tintDecay * dt;

            System.Array.Copy(_tints, _tintBuffer, count);

            for (int i = 0; i < count; i++)
            {
                int x = i % vcx;
                int z = i / vcx;
                int neighbors = 0;
                Color sum = Color.clear;

                if (x > 0) { sum += _tintBuffer[i - 1]; neighbors++; }
                if (x < _resX) { sum += _tintBuffer[i + 1]; neighbors++; }
                if (z > 0) { sum += _tintBuffer[i - vcx]; neighbors++; }
                if (z < _resZ) { sum += _tintBuffer[i + vcx]; neighbors++; }

                Color avg = sum / neighbors;
                _tints[i] = Color.Lerp(_tints[i], avg, diffuse);
                _tints[i] = Color.Lerp(_tints[i], Color.clear, decay);
            }
        }

        private void ComputeVertexColors()
        {
            for (int i = 0; i < _colors.Length; i++)
            {
                float disp = Mathf.Abs(_positions[i].y - _restPositions[i].y);
                float speed = _velocities[i].magnitude;
                float energy = Mathf.Clamp01(disp * glowFromDisplacement + speed * glowFromVelocity);

                _colors[i] = new Color(
                    _tints[i].r * energy,
                    _tints[i].g * energy,
                    _tints[i].b * energy,
                    energy
                );
            }
        }

        private void UpdateMesh()
        {
            _mesh.vertices = _positions;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }
    }
}
