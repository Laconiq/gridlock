using UnityEngine;

namespace AIWE.Visual
{
    public enum ShapeType { Triangle, Diamond, Circle, Hexagon, Square }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GeometricShape : MonoBehaviour
    {
        [SerializeField] private ShapeType shapeType = ShapeType.Triangle;
        [SerializeField] private float size = 0.4f;

        public void SetShape(ShapeType type, float shapeSize)
        {
            shapeType = type;
            size = shapeSize;
            GenerateMesh();
        }

        private void Awake()
        {
            GenerateMesh();
        }

        private void GenerateMesh()
        {
            var mesh = shapeType switch
            {
                ShapeType.Triangle => CreateTetrahedron(),
                ShapeType.Diamond => CreateOctahedron(),
                ShapeType.Circle => CreateSphere(12, 8),
                ShapeType.Hexagon => CreateHexPrism(),
                ShapeType.Square => CreateCube(),
                _ => CreateTetrahedron()
            };

            GetComponent<MeshFilter>().mesh = mesh;
        }

        private Mesh CreateTetrahedron()
        {
            var mesh = new Mesh { name = "Tetrahedron" };
            float h = size * 0.816f;
            float r = size * 0.577f;

            var v0 = new Vector3(0, h * 0.75f, 0);
            var v1 = new Vector3(-size * 0.5f, -h * 0.25f, -r * 0.5f);
            var v2 = new Vector3(size * 0.5f, -h * 0.25f, -r * 0.5f);
            var v3 = new Vector3(0, -h * 0.25f, r);

            mesh.vertices = new[]
            {
                v0, v1, v2,
                v0, v2, v3,
                v0, v3, v1,
                v1, v3, v2
            };
            mesh.triangles = new[] { 0,1,2, 3,4,5, 6,7,8, 9,10,11 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateOctahedron()
        {
            var mesh = new Mesh { name = "Octahedron" };
            var top = new Vector3(0, size, 0);
            var bot = new Vector3(0, -size, 0);
            var f = new Vector3(0, 0, size * 0.7f);
            var b = new Vector3(0, 0, -size * 0.7f);
            var l = new Vector3(-size * 0.7f, 0, 0);
            var r = new Vector3(size * 0.7f, 0, 0);

            mesh.vertices = new[]
            {
                top,f,r, top,r,b, top,b,l, top,l,f,
                bot,r,f, bot,b,r, bot,l,b, bot,f,l
            };
            mesh.triangles = new[]
            {
                0,1,2, 3,4,5, 6,7,8, 9,10,11,
                12,13,14, 15,16,17, 18,19,20, 21,22,23
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateSphere(int lonSegments, int latSegments)
        {
            var mesh = new Mesh { name = "Sphere" };
            int vertCount = (lonSegments + 1) * (latSegments + 1);
            var verts = new Vector3[vertCount];
            var tris = new int[lonSegments * latSegments * 6];

            int vi = 0;
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = Mathf.PI * lat / latSegments;
                float sinT = Mathf.Sin(theta);
                float cosT = Mathf.Cos(theta);

                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegments;
                    verts[vi++] = new Vector3(
                        size * sinT * Mathf.Cos(phi),
                        size * cosT,
                        size * sinT * Mathf.Sin(phi)
                    );
                }
            }

            int ti = 0;
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int curr = lat * (lonSegments + 1) + lon;
                    int next = curr + lonSegments + 1;

                    tris[ti++] = curr;
                    tris[ti++] = next;
                    tris[ti++] = curr + 1;

                    tris[ti++] = curr + 1;
                    tris[ti++] = next;
                    tris[ti++] = next + 1;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateHexPrism()
        {
            var mesh = new Mesh { name = "HexPrism" };
            float h = size * 0.6f;
            int sides = 6;
            var verts = new Vector3[(sides + 1) * 2 + sides * 4];
            var tris = new int[sides * 12];

            // Top center + bottom center
            verts[0] = new Vector3(0, h, 0);
            verts[1] = new Vector3(0, -h, 0);

            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;
                float x = Mathf.Cos(angle) * size;
                float z = Mathf.Sin(angle) * size;
                verts[2 + i] = new Vector3(x, h, z);
                verts[2 + sides + i] = new Vector3(x, -h, z);
            }

            int vi = 2 + sides * 2;
            int ti = 0;

            // Top cap
            for (int i = 0; i < sides; i++)
            {
                tris[ti++] = 0;
                tris[ti++] = 2 + i;
                tris[ti++] = 2 + (i + 1) % sides;
            }

            // Bottom cap
            for (int i = 0; i < sides; i++)
            {
                tris[ti++] = 1;
                tris[ti++] = 2 + sides + (i + 1) % sides;
                tris[ti++] = 2 + sides + i;
            }

            // Sides
            for (int i = 0; i < sides; i++)
            {
                int topCurr = 2 + i;
                int topNext = 2 + (i + 1) % sides;
                int botCurr = 2 + sides + i;
                int botNext = 2 + sides + (i + 1) % sides;

                tris[ti++] = topCurr;
                tris[ti++] = botCurr;
                tris[ti++] = topNext;

                tris[ti++] = topNext;
                tris[ti++] = botCurr;
                tris[ti++] = botNext;
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        private Mesh CreateCube()
        {
            var mesh = new Mesh { name = "Cube" };
            float s = size;
            mesh.vertices = new[]
            {
                // Front
                new Vector3(-s, -s, s), new Vector3(s, -s, s), new Vector3(s, s, s), new Vector3(-s, s, s),
                // Back
                new Vector3(s, -s, -s), new Vector3(-s, -s, -s), new Vector3(-s, s, -s), new Vector3(s, s, -s),
                // Top
                new Vector3(-s, s, s), new Vector3(s, s, s), new Vector3(s, s, -s), new Vector3(-s, s, -s),
                // Bottom
                new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, -s, s), new Vector3(-s, -s, s),
                // Right
                new Vector3(s, -s, s), new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(s, s, s),
                // Left
                new Vector3(-s, -s, -s), new Vector3(-s, -s, s), new Vector3(-s, s, s), new Vector3(-s, s, -s)
            };
            mesh.triangles = new[]
            {
                0,2,1, 0,3,2,
                4,6,5, 4,7,6,
                8,10,9, 8,11,10,
                12,14,13, 12,15,14,
                16,18,17, 16,19,18,
                20,22,21, 20,23,22
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
