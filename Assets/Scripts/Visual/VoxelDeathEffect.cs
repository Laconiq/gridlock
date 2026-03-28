using UnityEngine;

namespace AIWE.Visual
{
    public class VoxelDeathEffect : MonoBehaviour
    {
        [SerializeField] private float voxelSize = 0.07f;
        [SerializeField] private float explosionForce = 3f;
        [SerializeField] private float voxelLifetime = 1.2f;

        private Enemies.EnemyHealth _health;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _health = GetComponent<Enemies.EnemyHealth>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.OnDeath -= OnDeath;
        }

        private void OnDeath()
        {
            if (_meshFilter == null || _meshRenderer == null) return;

            var mesh = _meshFilter.sharedMesh;
            if (mesh == null) return;

            _meshRenderer.enabled = false;

            var material = _meshRenderer.sharedMaterial;
            var worldBounds = _meshRenderer.bounds;
            var center = worldBounds.center;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var localToWorld = transform.localToWorldMatrix;
            var worldToLocal = transform.worldToLocalMatrix;

            float floorY = transform.position.y - worldBounds.extents.y;

            for (float x = worldBounds.min.x; x <= worldBounds.max.x; x += voxelSize)
            for (float y = worldBounds.min.y; y <= worldBounds.max.y; y += voxelSize)
            for (float z = worldBounds.min.z; z <= worldBounds.max.z; z += voxelSize)
            {
                var worldPoint = new Vector3(x, y, z);
                var localPoint = worldToLocal.MultiplyPoint3x4(worldPoint);

                if (!IsInsideMesh(localPoint, vertices, triangles))
                    continue;

                var dir = (worldPoint - center);
                if (dir.sqrMagnitude < 0.0001f)
                    dir = Random.onUnitSphere;
                else
                    dir = dir.normalized;

                dir += Random.insideUnitSphere * 0.5f;
                var velocity = dir * explosionForce + Vector3.up * Random.Range(1f, 3f);

                var go = new GameObject("Voxel");
                go.transform.position = worldPoint;
                go.transform.rotation = Random.rotation;

                var vp = go.AddComponent<VoxelParticle>();
                vp.Initialize(velocity, voxelSize, voxelLifetime, material, floorY);
            }
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
                {
                    intersections++;
                }
            }

            return intersections % 2 == 1;
        }

        private static bool RayTriangleIntersect(Vector3 origin, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            var e1 = v1 - v0;
            var e2 = v2 - v0;
            var h = Vector3.Cross(dir, e2);
            float a = Vector3.Dot(e1, h);

            if (a > -0.00001f && a < 0.00001f)
                return false;

            float f = 1f / a;
            var s = origin - v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0f || u > 1f)
                return false;

            var q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(dir, q);

            if (v < 0f || u + v > 1f)
                return false;

            float t = f * Vector3.Dot(e2, q);
            return t > 0.00001f;
        }
    }
}
