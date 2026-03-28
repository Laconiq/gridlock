using Gridlock.Core;
using Gridlock.Grid;
using UnityEngine;

namespace Gridlock.Visual
{
    public class VoxelPool : MonoBehaviour
    {
        public static VoxelPool Instance { get; private set; }

        [SerializeField] private int maxVoxels = 4096;
        [SerializeField] private float settleTime = 2f;

        private Vector3[] _positions;
        private Vector3[] _velocities;
        private Quaternion[] _rotations;
        private Vector3[] _rotAxes;
        private float[] _angSpeeds;
        private float[] _sizes;
        private float[] _floorYs;
        private float[] _ages;
        private bool[] _alive;
        private int _count;

        private Mesh _cubeMesh;
        private Material _material;
        private readonly Matrix4x4[] _batch = new Matrix4x4[1023];

        private void Awake()
        {
            Instance = this;
            _positions = new Vector3[maxVoxels];
            _velocities = new Vector3[maxVoxels];
            _rotations = new Quaternion[maxVoxels];
            _rotAxes = new Vector3[maxVoxels];
            _angSpeeds = new float[maxVoxels];
            _sizes = new float[maxVoxels];
            _floorYs = new float[maxVoxels];
            _ages = new float[maxVoxels];
            _alive = new bool[maxVoxels];

            _cubeMesh = VoxelParticle.SharedCubeMesh;

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState prev, GameState current)
        {
            if (current == GameState.Preparing)
                ClearAll();
        }

        public void SetMaterial(Material mat)
        {
            if (_material == null)
            {
                _material = new Material(mat);
                _material.enableInstancing = true;
                _material.renderQueue = 1999;
            }
        }

        public void Spawn(Vector3 pos, Vector3 velocity, float size, Material mat, float floorY)
        {
            if (_count >= maxVoxels)
                RecycleOldest();

            if (_material == null && mat != null)
                _material = new Material(mat);

            int i = _count;
            _positions[i] = pos;
            _velocities[i] = velocity;
            _rotations[i] = Random.rotation;
            _rotAxes[i] = Random.onUnitSphere;
            _angSpeeds[i] = Random.Range(180f, 540f);
            _sizes[i] = size;
            _floorYs[i] = floorY;
            _ages[i] = 0f;
            _alive[i] = true;
            _count++;
        }

        private void RecycleOldest()
        {
            float oldest = 0f;
            int idx = 0;
            for (int i = 0; i < _count; i++)
            {
                if (_ages[i] > oldest)
                {
                    oldest = _ages[i];
                    idx = i;
                }
            }
            RemoveAt(idx);
        }

        private void RemoveAt(int idx)
        {
            int last = _count - 1;
            if (idx < last)
            {
                _positions[idx] = _positions[last];
                _velocities[idx] = _velocities[last];
                _rotations[idx] = _rotations[last];
                _rotAxes[idx] = _rotAxes[last];
                _angSpeeds[idx] = _angSpeeds[last];
                _sizes[idx] = _sizes[last];
                _floorYs[idx] = _floorYs[last];
                _ages[idx] = _ages[last];
                _alive[idx] = _alive[last];
            }
            _count--;
        }

        private void LateUpdate()
        {
            if (_count == 0 || _cubeMesh == null || _material == null) return;

            float dt = Time.deltaTime;
            var warp = GridWarpManager.Instance;

            for (int i = 0; i < _count; i++)
            {
                _ages[i] += dt;

                if (_ages[i] < settleTime)
                {
                    _velocities[i].y -= 9.81f * dt;
                    _positions[i] += _velocities[i] * dt;
                    _rotations[i] = _rotations[i] * Quaternion.AngleAxis(_angSpeeds[i] * dt, _rotAxes[i]);

                    float floor = _floorYs[i];
                    if (warp != null)
                        floor += warp.GetWarpOffset(_positions[i].x, _positions[i].z);

                    if (_positions[i].y < floor)
                    {
                        _positions[i].y = floor;
                        _velocities[i].y *= -0.25f;
                        _velocities[i].x *= 0.6f;
                        _velocities[i].z *= 0.6f;
                        _angSpeeds[i] *= 0.7f;
                    }
                }
                else if (warp != null)
                {
                    float floor = _floorYs[i] + warp.GetWarpOffset(_positions[i].x, _positions[i].z);
                    _positions[i].y = floor;
                }
            }

            Render();
        }

        private void Render()
        {
            int rendered = 0;

            for (int i = 0; i < _count; i++)
            {
                _batch[rendered] = Matrix4x4.TRS(_positions[i], _rotations[i], Vector3.one * _sizes[i]);
                rendered++;

                if (rendered >= 1023)
                {
                    Graphics.DrawMeshInstanced(_cubeMesh, 0, _material, _batch, rendered);
                    rendered = 0;
                }
            }

            if (rendered > 0)
                Graphics.DrawMeshInstanced(_cubeMesh, 0, _material, _batch, rendered);
        }

        private void ClearAll()
        {
            _count = 0;
        }
    }
}
