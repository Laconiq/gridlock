using UnityEngine;

namespace AIWE.Visual
{
    public class VoxelParticle : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _angularSpeed;
        private Vector3 _rotationAxis;
        private float _lifetime;
        private float _maxLifetime;
        private float _size;
        private float _floorY;

        private static Mesh _sharedCubeMesh;

        public static Mesh SharedCubeMesh
        {
            get
            {
                if (_sharedCubeMesh == null)
                    _sharedCubeMesh = CreateUnitCube();
                return _sharedCubeMesh;
            }
        }

        public void Initialize(Vector3 velocity, float size, float lifetime, Material material, float floorY)
        {
            _velocity = velocity;
            _size = size;
            _maxLifetime = lifetime;
            _floorY = floorY;

            _angularSpeed = Random.Range(360f, 720f);
            _rotationAxis = Random.onUnitSphere;

            transform.localScale = Vector3.one * size;

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = SharedCubeMesh;

            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private void Update()
        {
            _lifetime += Time.deltaTime;
            float t = _lifetime / _maxLifetime;

            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            _velocity.y -= 9.81f * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            // Grid warp floor
            float warpY = 0f;
            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warpY = warp.GetWarpOffset(transform.position.x, transform.position.z);

            float floor = _floorY + warpY;

            if (transform.position.y < floor)
            {
                var pos = transform.position;
                pos.y = floor;
                transform.position = pos;
                _velocity.y *= -0.3f;
                _velocity.x *= 0.7f;
                _velocity.z *= 0.7f;
            }

            transform.Rotate(_rotationAxis, _angularSpeed * Time.deltaTime, Space.World);

            float scale = _size * (1f - t * t);
            transform.localScale = Vector3.one * scale;
        }

        private static Mesh CreateUnitCube()
        {
            var mesh = new Mesh { name = "VoxelCube" };
            const float s = 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-s, -s, s), new Vector3(s, -s, s), new Vector3(s, s, s), new Vector3(-s, s, s),
                new Vector3(s, -s, -s), new Vector3(-s, -s, -s), new Vector3(-s, s, -s), new Vector3(s, s, -s),
                new Vector3(-s, s, s), new Vector3(s, s, s), new Vector3(s, s, -s), new Vector3(-s, s, -s),
                new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, -s, s), new Vector3(-s, -s, s),
                new Vector3(s, -s, s), new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(s, s, s),
                new Vector3(-s, -s, -s), new Vector3(-s, -s, s), new Vector3(-s, s, s), new Vector3(-s, s, -s)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21, 20, 23, 22
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
