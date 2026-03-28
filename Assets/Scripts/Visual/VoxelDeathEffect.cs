using UnityEngine;

namespace Gridlock.Visual
{
    public class VoxelDeathEffect : MonoBehaviour
    {
        [SerializeField] private float voxelSize = 0.3f;
        [SerializeField] private float hitShedForce = 1.5f;
        [SerializeField] private float deathExplosionForce = 3f;

        private Enemies.EnemyHealth _health;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Vector3[] _voxelLocalPositions;
        private int _totalVoxels;
        private int _shedIndex;

        private void Awake()
        {
            _health = GetComponent<Enemies.EnemyHealth>();
            _meshFilter = GetComponentInChildren<MeshFilter>();
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        private void Start()
        {
            PrecomputeVoxelPositions();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health._currentHPChanged += OnHit;
                _health.OnDeath += OnDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health._currentHPChanged -= OnHit;
                _health.OnDeath -= OnDeath;
            }
        }

        private void PrecomputeVoxelPositions()
        {
            if (_meshFilter == null) return;
            var mesh = _meshFilter.sharedMesh;
            if (mesh == null) return;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var bounds = mesh.bounds;

            var tempList = new System.Collections.Generic.List<Vector3>(64);

            for (float x = bounds.min.x; x <= bounds.max.x; x += voxelSize)
            for (float y = bounds.min.y; y <= bounds.max.y; y += voxelSize)
            for (float z = bounds.min.z; z <= bounds.max.z; z += voxelSize)
            {
                var p = new Vector3(x, y, z);
                if (IsInsideMesh(p, vertices, triangles))
                    tempList.Add(p);
            }

            _voxelLocalPositions = tempList.ToArray();
            _totalVoxels = _voxelLocalPositions.Length;

            // Shuffle for random shedding order
            for (int i = _totalVoxels - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_voxelLocalPositions[i], _voxelLocalPositions[j]) = (_voxelLocalPositions[j], _voxelLocalPositions[i]);
            }
        }

        private void OnHit(float damage)
        {
            if (_totalVoxels == 0 || _health == null) return;

            float percent = damage / _health.MaxHP;
            int toShed = Mathf.Clamp(Mathf.RoundToInt(percent * _totalVoxels * 0.08f), 1, 3);
            ShedVoxels(toShed, hitShedForce);
        }

        private void OnDeath()
        {
            int remaining = _totalVoxels - _shedIndex;
            int toSpawn = Mathf.Min(remaining, 8);
            if (toSpawn > 0)
                ShedVoxels(toSpawn, deathExplosionForce);

            if (_meshRenderer != null)
                _meshRenderer.enabled = false;
        }

        private void ShedVoxels(int count, float force)
        {
            var pool = VoxelPool.Instance;
            if (pool == null || _meshRenderer == null) return;

            var mat = _meshRenderer.sharedMaterial;
            pool.SetMaterial(mat);

            var modelTransform = _meshFilter.transform;
            var worldBounds = _meshRenderer.bounds;
            var center = worldBounds.center;
            float floorY = transform.position.y - worldBounds.extents.y;

            int end = Mathf.Min(_shedIndex + count, _totalVoxels);

            for (int i = _shedIndex; i < end; i++)
            {
                var worldPos = modelTransform.TransformPoint(_voxelLocalPositions[i]);

                var dir = (worldPos - center);
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Random.onUnitSphere;
                else
                    dir = dir.normalized;

                dir += Random.insideUnitSphere * 0.5f;
                var velocity = dir * force + Vector3.up * Random.Range(0.5f, 2f);

                pool.Spawn(worldPos, velocity, voxelSize, mat, floorY);
            }

            _shedIndex = end;
        }

        private static bool IsInsideMesh(Vector3 point, Vector3[] vertices, int[] triangles)
        {
            int intersections = 0;
            var rayDir = new Vector3(1f, 0.0001f, 0.0001f);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (RayTriangleIntersect(point, rayDir,
                        vertices[triangles[i]],
                        vertices[triangles[i + 1]],
                        vertices[triangles[i + 2]]))
                    intersections++;
            }

            return intersections % 2 == 1;
        }

        private static bool RayTriangleIntersect(Vector3 origin, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var e1 = v1 - v0;
            var e2 = v2 - v0;
            var h = Vector3.Cross(dir, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -0.00001f && a < 0.00001f) return false;

            float f = 1f / a;
            var s = origin - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0f || u > 1f) return false;

            var q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(dir, q);
            if (v < 0f || u + v > 1f) return false;

            return f * Vector3.Dot(e2, q) > 0.00001f;
        }
    }
}
